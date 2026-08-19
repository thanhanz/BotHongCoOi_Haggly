using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Haggly.Api;
using Haggly.Api.Endpoints.Catalog;
using Haggly.Api.Responses;
using Haggly.Application.Modules.Catalog.Dtos.Categories;
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
    public async Task HttpPipeline_WhenContributorCreatesCategory_ReturnsCreatedCategory(RoleCode role)
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

        //Create a new one
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
        var response = await created.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("Category created successfully.", response.Message);
        Assert.Equal("Integration Category", response.Data.Name);
        Assert.Equal(slug, response.Data.Slug);
        Assert.Equal(0, response.Data.DisplayOrder);
        Assert.Equal($"/api/v1/categories/{response.Data.Id}", created.Headers.Location?.OriginalString);
    }
}
