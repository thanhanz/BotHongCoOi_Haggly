using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Haggly.Api;
using Haggly.Api.Endpoints.Inventory;
using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Inventory;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Authentication;
using Haggly.IntegrationTests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Inventory;

public sealed class InventoryApiIntegrationTests
{
    [Fact]
    public async Task InventoryLifecycle_WhenVendorUsesOwnedStall_ExecutesAndPersistsTheFullFlow()
    {
        await using var app = await CreateAppAsync();
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        using var client = CreateClient(app);
        var token = CreateToken(scenario.OwnerId);
        var route = InventoryRoutes.Prefix.Replace(
            "{stallId:guid}",
            scenario.StallId.ToString());

        using var openRequest = CreateRequest(
            HttpMethod.Post,
            route + InventoryRoutes.OpenSession,
            token,
            new
            {
                notes = "Integration opening",
                listings = new[]
                {
                    new
                    {
                        productStallId = scenario.ProductStallId,
                        openingQuantity = 10m,
                        publicUnitPrice = 45m
                    }
                }
            });
        var opened = await client.SendAsync(openRequest);
        Assert.Equal(HttpStatusCode.Created, opened.StatusCode);
        var openedEnvelope = await opened.Content.ReadFromJsonAsync<SessionEnvelope>();
        var listing = Assert.Single(openedEnvelope!.Data.Listings);
        Assert.Equal(10m, listing.CurrentQuantity);

        var current = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            route + InventoryRoutes.CurrentSession,
            token));
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);

        var patched = await client.SendAsync(CreateRequest(
            HttpMethod.Patch,
            route + InventoryRoutes.ListingById.Replace("{listingId:guid}", listing.Id.ToString()),
            token,
            new { publicUnitPrice = 50m, status = DailyListingStatus.AVAILABLE, expectedVersion = listing.Version }));
        var patchedBody = await patched.Content.ReadAsStringAsync();
        Assert.True(patched.StatusCode == HttpStatusCode.OK, patchedBody);
        var patchedEnvelope = await patched.Content.ReadFromJsonAsync<ListingEnvelope>();
        Assert.Equal(50m, patchedEnvelope!.Data.PublicUnitPrice);

        var adjusted = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            route + InventoryRoutes.Adjustments,
            token,
            new
            {
                listingId = listing.Id,
                quantityDelta = 2m,
                reason = "Integration restock",
                expectedVersion = patchedEnvelope.Data.Version
            }));
        Assert.Equal(HttpStatusCode.OK, adjusted.StatusCode);
        var adjustedEnvelope = await adjusted.Content.ReadFromJsonAsync<ListingEnvelope>();
        Assert.Equal(12m, adjustedEnvelope!.Data.CurrentQuantity);

        var ledger = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            route + InventoryRoutes.Ledger
                + $"?businessDate={openedEnvelope.Data.BusinessDate:yyyy-MM-dd}&listingId={listing.Id}",
            token));
        Assert.Equal(HttpStatusCode.OK, ledger.StatusCode);
        Assert.Contains("Integration restock", await ledger.Content.ReadAsStringAsync());

        var closed = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            route + InventoryRoutes.CloseSession,
            token));
        Assert.Equal(HttpStatusCode.OK, closed.StatusCode);
        var closedEnvelope = await closed.Content.ReadFromJsonAsync<SessionEnvelope>();
        Assert.Equal(InventorySessionStatus.CLOSED, closedEnvelope!.Data.Status);

        var secondClose = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            route + InventoryRoutes.CloseSession,
            token));
        Assert.Equal(HttpStatusCode.Conflict, secondClose.StatusCode);
    }

    [Fact]
    public async Task InventoryRoute_WhenCallerDoesNotOwnStall_ReturnsForbidden()
    {
        await using var app = await CreateAppAsync();
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        using var client = CreateClient(app);
        var route = InventoryRoutes.Prefix.Replace(
            "{stallId:guid}",
            scenario.StallId.ToString()) + InventoryRoutes.CurrentSession;

        var response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            route,
            CreateToken(Guid.NewGuid())));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:HagglyDatabase"] = IntegrationTestDatabase.ConnectionString,
            ["Jwt:Issuer"] = "Haggly.Api.Tests",
            ["Jwt:Audience"] = "Haggly.Client.Tests",
            ["Jwt:SigningKey"] = "integration-test-signing-key-that-is-at-least-32-characters",
            ["Jwt:AccessTokenMinutes"] = "15"
        });
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddTokenServices(builder.Configuration);
        builder.Services.AddApiServices();
        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapInventoryEndpoints();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
        => new()
        {
            BaseAddress = new Uri(app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses.Single())
        };

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path,
        string token,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static string CreateToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            "integration-test-signing-key-that-is-at-least-32-characters"));
        var token = new JwtSecurityToken(
            "Haggly.Api.Tests",
            "Haggly.Client.Tests",
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("roles", RoleCode.VENDOR.ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record SessionEnvelope(SessionData Data);

    private sealed record ListingEnvelope(ListingData Data);

    private sealed record SessionData(
        Guid Id,
        DateOnly BusinessDate,
        InventorySessionStatus Status,
        IReadOnlyCollection<ListingData> Listings);

    private sealed record ListingData(
        Guid Id,
        decimal PublicUnitPrice,
        decimal CurrentQuantity,
        long Version);
}
