using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Dapper;
using Haggly.Api;
using Haggly.Api.Endpoints.Sales;
using Haggly.Api.Responses;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Sales;
using Haggly.Infrastructure.Authentication;
using Haggly.Infrastructure.Persistence;
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

public sealed class OrderApiIntegrationTests
{
    [Fact]
    public async Task OrderLifecycle_WhenBuyerCreatesAndCancelsOrder_ReturnsBuyerOwnedResults()
    {
        await using var app = await CreateAppAsync();
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        var buyerId = await SeedBuyerAsync();
        var inventoryItemId = Guid.NewGuid();
        await SeedInventoryItemAsync(scenario, inventoryItemId);
        using var client = CreateClient(app);
        var token = CreateToken(buyerId);

        using var create = new HttpRequestMessage(HttpMethod.Post, OrderRoutes.Prefix)
        {
            Content = JsonContent.Create(new
            {
                items = new[]
                {
                    new { inventoryItemId, quantity = 2m, notes = "Packed separately" }
                }
            })
        };
        create.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var created = await client.SendAsync(create);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var orderId = Guid.Parse(created.Headers.Location!.OriginalString.TrimEnd('/').Split('/')[^1]);

        using var detail = new HttpRequestMessage(HttpMethod.Get, $"{OrderRoutes.Prefix}/{orderId}");
        detail.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var detailResponse = await client.SendAsync(detail);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.Contains(inventoryItemId.ToString(), await detailResponse.Content.ReadAsStringAsync());

        using var list = new HttpRequestMessage(HttpMethod.Get, OrderRoutes.Prefix);
        list.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var listResponse = await client.SendAsync(list);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(orderId.ToString(), await listResponse.Content.ReadAsStringAsync());

        using var cancel = new HttpRequestMessage(HttpMethod.Post, $"{OrderRoutes.Prefix}/{orderId}/cancel")
        {
            Content = JsonContent.Create(new { reason = "Buyer changed their mind" })
        };
        cancel.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var cancelled = await client.SendAsync(cancel);
        var cancelledResponse = await cancelled.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();

        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.NotNull(cancelledResponse);
        Assert.True(cancelledResponse.Success);
        Assert.Equal(OrderStatus.CANCELLED, cancelledResponse.Data.Status);
        Assert.Equal("Buyer changed their mind", cancelledResponse.Data.CancellationReason);
        Assert.NotNull(cancelledResponse.Data.CancelledAt);

        var db = new DapperDbContext(IntegrationTestDatabase.CreateConfiguration());
        await using var connection = await db.OpenConnectionAsync(CancellationToken.None);
        var status = await connection.ExecuteScalarAsync<string>(
            "SELECT \"Status\" FROM sales.orders WHERE \"Id\" = @orderId",
            new { orderId });
        Assert.Equal("CANCELLED", status);
    }

    [Fact]
    public async Task OrderDetails_WhenAnotherBuyerRequestsOrder_ReturnsForbidden()
    {
        await using var app = await CreateAppAsync();
        var scenario = await InventoryIntegrationScenarioFactory.CreateAsync();
        var ownerBuyerId = await SeedBuyerAsync();
        var otherBuyerId = await SeedBuyerAsync();
        var inventoryItemId = Guid.NewGuid();
        await SeedInventoryItemAsync(scenario, inventoryItemId);
        using var client = CreateClient(app);

        var orderId = await CreateOrderAsync(client, ownerBuyerId, inventoryItemId);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{OrderRoutes.Prefix}/{orderId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(otherBuyerId));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<Guid> SeedBuyerAsync()
    {
        var buyerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var db = new DapperDbContext(IntegrationTestDatabase.CreateConfiguration());
        await using var connection = await db.OpenConnectionAsync(CancellationToken.None);
        await connection.ExecuteAsync(
            """
            INSERT INTO identity.users
                ("Id", "Email", "PhoneNumber", "PasswordHash", "FullName", "Status", "CreatedAt")
            VALUES
                (@BuyerId, @Email, '', 'integration-test', 'Integration Buyer', 'ACTIVE', @Now);
            INSERT INTO identity.buyer_profiles ("UserId", "CreatedAt")
            VALUES (@BuyerId, @Now);
            """,
            new
            {
                BuyerId = buyerId,
                Email = $"buyer-{buyerId:N}@integration.test",
                Now = now
            });
        return buyerId;
    }

    private static async Task SeedInventoryItemAsync(
        InventoryIntegrationScenario scenario,
        Guid inventoryItemId)
    {
        var db = new DapperDbContext(IntegrationTestDatabase.CreateConfiguration());
        await using var connection = await db.OpenConnectionAsync(CancellationToken.None);
        await connection.ExecuteAsync(
            """
            INSERT INTO inventory.inventory_items
                ("Id", "InventoryId", "ProductStallId", "CurrentQuantity", "ReservedQuantity", "Version", "CreatedAt", "CreatedBy")
            VALUES
                (@InventoryItemId, @InventoryId, @ProductStallId, 10.000, 0.000, 0, @Now, @OwnerId);
            """,
            new
            {
                InventoryItemId = inventoryItemId,
                scenario.InventoryId,
                scenario.ProductStallId,
                scenario.OwnerId,
                Now = DateTimeOffset.UtcNow
            });
    }

    private static async Task<Guid> CreateOrderAsync(
        HttpClient client,
        Guid buyerId,
        Guid inventoryItemId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, OrderRoutes.Prefix)
        {
            Content = JsonContent.Create(new
            {
                items = new[] { new { inventoryItemId, quantity = 1m } }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(buyerId));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Guid.Parse(response.Headers.Location!.OriginalString.TrimEnd('/').Split('/')[^1]);
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
        app.MapOrderEndpoints();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
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
                new Claim("roles", RoleCode.BUYER.ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
