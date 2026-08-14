using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dapper;
using Haggly.Api;
using Haggly.Api.Endpoints.Catalog;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Authentication;
using Haggly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Catalog;

public sealed class ProductApiIntegrationTests
{
    [Theory]
    [InlineData(RoleCode.VENDOR)]
    [InlineData(RoleCode.MARKET_ADMIN)]
    [InlineData(RoleCode.PLATFORM_ADMIN)]
    public async Task HttpPipeline_WhenContributorCreatesProduct_PersistsAndReturnsCreated(RoleCode role)
    {
        await using var app = await CreateAppAsync();
        var category = await SeedActiveCategoryAsync(app.Services);
        using var client = CreateClient(app);
        var token = CreateToken(app.Services, role);
        var productName = $"Product-{Guid.NewGuid():N}";

        var created = await PostProductAsync(client, token, category.Id, productName);
        var products = await GetProductsAsync(client, token, category.Id);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, products.StatusCode);
        Assert.Contains(productName, await products.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task HttpPipeline_WhenProductCategoryIsUnknown_ReturnsNotFound()
    {
        await using var app = await CreateAppAsync();
        using var client = CreateClient(app);
        var token = CreateToken(app.Services, RoleCode.VENDOR);

        var response = await PostProductAsync(
            client,
            token,
            Guid.NewGuid(),
            $"Product-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HttpPipeline_WhenProductNameDuplicatesWithinCategory_ReturnsConflictButAnotherCategoryAllowsIt()
    {
        await using var app = await CreateAppAsync();
        var firstCategory = await SeedActiveCategoryAsync(app.Services);
        var secondCategory = await SeedActiveCategoryAsync(app.Services);
        using var client = CreateClient(app);
        var token = CreateToken(app.Services, RoleCode.VENDOR);
        var productName = $"Product-{Guid.NewGuid():N}";

        var first = await PostProductAsync(client, token, firstCategory.Id, productName);
        var duplicate = await PostProductAsync(client, token, firstCategory.Id, productName);
        var otherCategory = await PostProductAsync(client, token, secondCategory.Id, productName);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Created, otherCategory.StatusCode);
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:HagglyDatabase"] =
                "Host=localhost;Port=5433;Database=haggly;Username=postgres;Password=1234",
            ["Jwt:Issuer"] = "Haggly.Api.Tests",
            ["Jwt:Audience"] = "Haggly.Client.Tests",
            ["Jwt:SigningKey"] = "integration-test-signing-key-that-is-at-least-32-characters",
            ["Jwt:AccessTokenMinutes"] = "15"
        });
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddTokenServices(builder.Configuration);
        builder.Services.AddApiServices();
        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapProductEndpoints();
        await app.StartAsync();

        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();

        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private static string CreateToken(IServiceProvider services, RoleCode role)
        => services.GetRequiredService<Haggly.Application.Abstractions.Identity.IIdentityTokenService>()
            .CreateAccessToken(new User { Email = $"{role}-{Guid.NewGuid():N}@example.com" }, [role])
            .Value;

    private static async Task<HttpResponseMessage> PostProductAsync(
        HttpClient client,
        string token,
        Guid categoryId,
        string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/products")
        {
            Content = JsonContent.Create(new
            {
                categoryId,
                name,
                defaultUnit = ProductUnit.KG
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> GetProductsAsync(
        HttpClient client,
        string token,
        Guid categoryId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/products?categoryId={categoryId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.SendAsync(request);
    }

    private static async Task<Category> SeedActiveCategoryAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DapperDbContext>();
        var category = new Category
        {
            Name = $"Category-{Guid.NewGuid():N}",
            Slug = $"category-{Guid.NewGuid():N}"
        };
        const string sql = """
            INSERT INTO catalog.categories
                ("Id", "Name", "Slug", "DisplayOrder", "Status", "CreatedAt")
            VALUES
                (@Id, @Name, @Slug, @DisplayOrder, @Status, @CreatedAt);
            """;

        await using var connection = await dbContext.OpenConnectionAsync(CancellationToken.None);
        await connection.ExecuteAsync(sql, new
        {
            category.Id,
            category.Name,
            category.Slug,
            category.DisplayOrder,
            Status = category.Status.ToString(),
            category.CreatedAt
        });

        return category;
    }
}
