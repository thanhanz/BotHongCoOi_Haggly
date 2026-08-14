using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Dapper;
using Haggly.Api.Endpoints.Catalog;
using Haggly.Api;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Identity;
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
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Catalog;

public sealed class ProductStallApiIntegrationTests
{
    [Fact]
    public async Task Post_WhenCallerOwnsStall_CreatesProductStallAndCanReadIt()
    {
        await using var app = await CreateAppAsync();
        var owner = await SeedScenarioAsync(app.Services);
        using var client = CreateClient(app);
        var response = await SendAsync(app.Services, client, HttpMethod.Post, $"/api/v1/stalls/{owner.StallId}/products",
            owner.UserId, new { productId = owner.ProductId, sellingUnit = ProductUnit.KG,
                minimumOrderQuantity = 1m, defaultUnitPrice = 25m, isNegotiable = true });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(owner.ProductId.ToString(), body);

        var get = await SendAsync(app.Services, client, HttpMethod.Get, $"/api/v1/stalls/{owner.StallId}/products", owner.UserId);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Contains(owner.ProductId.ToString(), await get.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_WhenCallerDoesNotOwnStall_ReturnsForbidden()
    {
        await using var app = await CreateAppAsync();
        var owner = await SeedScenarioAsync(app.Services);
        using var client = CreateClient(app);

        var response = await SendAsync(app.Services, client, HttpMethod.Post, $"/api/v1/stalls/{owner.StallId}/products",
            Guid.NewGuid(), new { productId = owner.ProductId, sellingUnit = ProductUnit.KG,
                minimumOrderQuantity = 1m, defaultUnitPrice = 25m, isNegotiable = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Patch_WhenCallerOwnsStall_UpdatesConfiguration()
    {
        await using var app = await CreateAppAsync();
        var owner = await SeedScenarioAsync(app.Services);
        using var client = CreateClient(app);
        var created = await SendAsync(app.Services, client, HttpMethod.Post, $"/api/v1/stalls/{owner.StallId}/products",
            owner.UserId, new { productId = owner.ProductId, sellingUnit = ProductUnit.KG,
                minimumOrderQuantity = 1m, defaultUnitPrice = 25m, isNegotiable = false });
        var createdJson = await created.Content.ReadFromJsonAsync<ApiEnvelope>();

        var response = await SendAsync(app.Services, client, HttpMethod.Patch,
            $"/api/v1/stalls/{owner.StallId}/products/{createdJson!.Data.Id}", owner.UserId,
            new { defaultUnitPrice = 30m, isActive = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("30", await response.Content.ReadAsStringAsync());
        Assert.Contains("false", (await response.Content.ReadAsStringAsync()).ToLowerInvariant());
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:HagglyDatabase"] = IntegrationTestDatabase.ConnectionString,
            ["Jwt:Issuer"] = "Haggly.Api.Tests", ["Jwt:Audience"] = "Haggly.Client.Tests",
            ["Jwt:SigningKey"] = "integration-test-signing-key-that-is-at-least-32-characters",
            ["Jwt:AccessTokenMinutes"] = "15"
        });
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddTokenServices(builder.Configuration);
        builder.Services.AddApiServices();
        var app = builder.Build();
        app.UseExceptionHandler(); app.UseAuthentication(); app.UseAuthorization();
        app.MapProductStallEndpoints();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private static async Task<HttpResponseMessage> SendAsync(IServiceProvider services, HttpClient client, HttpMethod method, string path, Guid userId, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(services, userId));
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static string CreateToken(IServiceProvider services, Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("integration-test-signing-key-that-is-at-least-32-characters"));
        var token = new JwtSecurityToken("Haggly.Api.Tests", "Haggly.Client.Tests",
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()), new Claim("roles", RoleCode.VENDOR.ToString())],
            expires: DateTime.UtcNow.AddMinutes(15), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record ApiEnvelope(ApiData Data);
    private sealed record ApiData(Guid Id);
    private sealed record Scenario(Guid UserId, Guid StallId, Guid ProductId);

    private static async Task<Scenario> SeedScenarioAsync(IServiceProvider services)
    {
        var userId = Guid.NewGuid(); var vendorId = userId; var marketId = Guid.NewGuid();
        var stallId = Guid.NewGuid(); var categoryId = Guid.NewGuid(); var productId = Guid.NewGuid();
        
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DapperDbContext>();

        await using var connection = await dbContext.OpenConnectionAsync(CancellationToken.None);
        var now = DateTimeOffset.UtcNow;
        await connection.ExecuteAsync("""
            INSERT INTO identity.users ("Id","Email","PhoneNumber","PasswordHash","FullName","Status","CreatedAt")
            VALUES (@userId,@email,'','test','Test Vendor','ACTIVE',@now);
            INSERT INTO identity.vendor_profiles ("UserId","BusinessName","ApprovalStatus","CreatedAt")
            VALUES (@userId,'Test Vendor','APPROVED',@now);
            INSERT INTO markets.markets ("Id","Code","Name","Address","Status","CreatedAt")
            VALUES (@marketId,@code,'Test Market','Address','ACTIVE',@now);
            INSERT INTO markets.stalls ("Id","MarketId","VendorId","Code","Name","Status","CreatedAt")
            VALUES (@stallId,@marketId,@userId,@stallCode,'Test Stall','ACTIVE',@now);
            INSERT INTO catalog.categories ("Id","Name","Slug","DisplayOrder","Status","CreatedAt")
            VALUES (@categoryId,'Test Category',@slug,0,'ACTIVE',@now);
            INSERT INTO catalog.products ("Id","CategoryId","Name","DefaultUnit","Status","CreatedAt")
            VALUES (@productId,@categoryId,@productName,'KG','ACTIVE',@now);
            """, new { userId, email = $"{userId}@example.com", marketId, code = $"M-{marketId:N}", stallId,
            stallCode = $"S-{stallId:N}", categoryId, slug = $"cat-{categoryId:N}", productId, productName = $"P-{productId:N}", now });
        return new(userId, stallId, productId);
    }
}
