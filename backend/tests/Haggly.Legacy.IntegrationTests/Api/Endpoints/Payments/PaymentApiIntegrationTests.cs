using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Dapper;
using Haggly.Api;
using Haggly.Api.Endpoints.Payments;
using Haggly.Api.Responses;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Modules.Payments.Dtos;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Payments;
using Haggly.Infrastructure.Authentication;
using Haggly.Infrastructure.Messaging.Outbox;
using Haggly.Infrastructure.Messaging.Serialization;
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

namespace Haggly.IntegrationTests.Api.Endpoints.Payments;

public sealed class PaymentApiIntegrationTests
{
    [Fact]
    public async Task StartPayment_WhenBuyerOwnsAgreedOrder_ReturnsAcceptedAndCommitsOutbox()
    {
        await using var app = await CreateAppAsync();
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        using var client = CreateClient(app);
        using var request = new HttpRequestMessage(HttpMethod.Post, PaymentRoutes.Prefix)
        {
            Content = JsonContent.Create(new { orderId })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(buyerId));

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.Equal(orderId, body.Data.OrderId);
        Assert.Equal(PaymentStatus.PENDING, body.Data.Status);
        Assert.Equal(300_000m, body.Data.AmountDue);
        Assert.Equal($"{PaymentRoutes.Prefix}/{body.Data.Id}", response.Headers.Location!.OriginalString);

        await using var connection = await OpenConnectionAsync();
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM messaging.outbox_messages WHERE \"CorrelationId\" = @PaymentId;",
            new { PaymentId = body.Data.Id }));
    }

    private static async Task<(Guid OrderId, Guid BuyerId)> CreateAgreedOrderAsync()
    {
        var buyerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using var connection = await OpenConnectionAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO identity.users
                ("Id", "Email", "PhoneNumber", "PasswordHash", "FullName", "Status", "CreatedAt")
            VALUES
                (@BuyerId, @Email, '', 'integration-test', 'Payment Buyer', 'ACTIVE', @Now);
            INSERT INTO identity.buyer_profiles ("UserId", "CreatedAt")
            VALUES (@BuyerId, @Now);
            INSERT INTO sales.orders
                ("Id", "OrderNo", "BuyerId", "Status", "TotalToCharge", "TotalPaid",
                 "Currency", "PlacedAt", "CreatedAt")
            VALUES
                (@OrderId, @OrderNo, @BuyerId, 'AGREED', 300000, 0, 'VND', @Now, @Now);
            """,
            new
            {
                BuyerId = buyerId,
                Email = $"payment-api-{buyerId:N}@integration.test",
                OrderId = orderId,
                OrderNo = $"ORD-{orderId:N}".ToUpperInvariant(),
                Now = now
            });
        return (orderId, buyerId);
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
        builder.Services.AddScoped<IOutboxWriter, DapperOutboxWriter>();
        builder.Services.AddSingleton(new DomainEventTypeRegistry(
        [
            DomainEventTypeRegistration.For<PaymentRequested>("payments.payment-requested.v1")
        ]));
        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapPaymentEndpoints();
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

    private static async Task<System.Data.Common.DbConnection> OpenConnectionAsync()
        => await new DapperDbContext(IntegrationTestDatabase.CreateConfiguration())
            .OpenConnectionAsync(CancellationToken.None);
}
