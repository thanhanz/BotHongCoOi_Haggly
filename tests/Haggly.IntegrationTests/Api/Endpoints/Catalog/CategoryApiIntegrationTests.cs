using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Haggly.Api;
using Haggly.Api.Endpoints.Catalog;
using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Authentication;
using Haggly.Infrastructure.Persistence;
using Haggly.IntegrationTests.Infrastructure.Persistence;
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

public sealed class CategoryApiIntegrationTests
{
    [Theory]
    [InlineData(RoleCode.VENDOR)]
    [InlineData(RoleCode.MARKET_ADMIN)]
    [InlineData(RoleCode.PLATFORM_ADMIN)]
    public async Task HttpPipeline_WhenContributorCreatesCategory_PersistsAndReturnsCreated(RoleCode role)
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
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddTokenServices(builder.Configuration);
        builder.Services.AddApiServices();
        await using var app = builder.Build();

        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCategoryEndpoints();

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var slug = $"category-{Guid.NewGuid():N}";
        var token = app.Services
            .GetRequiredService<Haggly.Application.Abstractions.Identity.IIdentityTokenService>()
            .CreateAccessToken(new User { Email = $"{role}@example.com" }, [role]);
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/categories")
        {
            Content = JsonContent.Create(new
            {
                name = "Integration Category",
                slug,
                displayOrder = 0
            })
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        var created = await client.SendAsync(createRequest);
        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/categories?page=1&pageSize=100");
        listRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        var categories = await client.SendAsync(listRequest);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
        Assert.Contains(slug, await categories.Content.ReadAsStringAsync());
    }
}
