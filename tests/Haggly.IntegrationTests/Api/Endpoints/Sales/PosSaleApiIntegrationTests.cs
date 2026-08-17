using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Dapper;
using Haggly.Api.Endpoints.Inventory;
using Haggly.Api.Endpoints.Sales;
using Haggly.Api;
using Haggly.Domain.Modules.Identity;
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
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Sales;

public sealed class PosSaleApiIntegrationTests
{
    [Fact]
    public async Task Post_WhenInventoryIsAvailable_CommitsSaleInventoryAndRevenue()
    {
        await using var app = await CreateAppAsync();
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        var inventoryItemId = Guid.NewGuid();
        await SeedInventoryItemAsync(scenario, inventoryItemId);
        using var client = CreateClient(app);

        var response = await SendAsync(
            app.Services,
            client,
            $"/api/v1/vendor/stalls/{scenario.StallId}/pos-sales",
            scenario.OwnerId,
            new
            {
                clientRequestId = $"client-{Guid.NewGuid():N}",
                items = new[]
                {
                    new
                    {
                        inventoryItemId,
                        quantity = 2m,
                        expectedInventoryVersion = 0L,
                        expectedProductStallVersion = 0L
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var db = new DapperDbContext(IntegrationTestDatabase.CreateConfiguration());
        await using var connection = await db.OpenConnectionAsync(CancellationToken.None);
        var quantity = await connection.ExecuteScalarAsync<decimal>(
            "SELECT \"CurrentQuantity\" FROM inventory.inventory_items WHERE \"Id\" = @inventoryItemId",
            new { inventoryItemId });
        var ledger = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM inventory.inventory_ledgers WHERE \"InventoryItemId\" = @inventoryItemId AND \"TransactionType\" = 'POS_SALE'",
            new { inventoryItemId });
        var revenue = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM finance.revenue_ledgers WHERE \"ReferenceType\" = 'POS_SALE' AND \"GrossAmount\" = 90.00",
            commandTimeout: 10);

        Assert.Equal(8m, quantity);
        Assert.Equal(1, ledger);
        Assert.True(revenue >= 1);
    }

    private static async Task SeedInventoryItemAsync(
        InventoryIntegrationScenario scenario,
        Guid inventoryItemId)
    {
        var db = new DapperDbContext(IntegrationTestDatabase.CreateConfiguration());
        await using var connection = await db.OpenConnectionAsync(CancellationToken.None);
        await connection.ExecuteAsync("""
            INSERT INTO inventory.inventory_items
                ("Id", "InventoryId", "ProductStallId", "CurrentQuantity", "ReservedQuantity", "Version", "CreatedAt", "CreatedBy")
            VALUES
                (@inventoryItemId, @inventoryId, @productStallId, 10.000, 0.000, 0, @now, @ownerId);
            """, new
        {
            inventoryItemId,
            inventoryId = scenario.InventoryId,
            productStallId = scenario.ProductStallId,
            ownerId = scenario.OwnerId,
            now = DateTimeOffset.UtcNow
        });
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
        app.MapPosSaleEndpoints();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private static async Task<HttpResponseMessage> SendAsync(
        IServiceProvider services,
        HttpClient client,
        string path,
        Guid userId,
        object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken(services, userId));
        return await client.SendAsync(request);
    }

    private static string CreateToken(IServiceProvider services, Guid userId)
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
}
