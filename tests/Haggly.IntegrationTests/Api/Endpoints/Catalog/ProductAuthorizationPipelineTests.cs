using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Haggly.Api;
using Haggly.Api.Endpoints.Catalog;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Catalog;

public sealed class ProductAuthorizationPipelineTests
{
    [Fact]
    public async Task HttpPipeline_WhenProductCreateIsUnauthenticatedOrBuyer_ReturnsUnauthorizedOrForbidden()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "Haggly.Api.Tests",
            ["Jwt:Audience"] = "Haggly.Client.Tests",
            ["Jwt:SigningKey"] = "integration-test-signing-key-that-is-at-least-32-characters",
            ["Jwt:AccessTokenMinutes"] = "15"
        });
        builder.Services.AddTokenServices(builder.Configuration);
        builder.Services.AddApiServices();
        await using var app = builder.Build();

        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapProductEndpoints();

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };

        var missing = await client.PostAsJsonAsync("/api/v1/products", new
        {
            categoryId = Guid.NewGuid(),
            name = "Apple",
            defaultUnit = ProductUnit.KG
        });
        var token = app.Services
            .GetRequiredService<Haggly.Application.Abstractions.Identity.IIdentityTokenService>()
            .CreateAccessToken(new User { Email = "buyer@example.com" }, [RoleCode.BUYER]);
        using var buyerRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/products")
        {
            Content = JsonContent.Create(new
            {
                categoryId = Guid.NewGuid(),
                name = "Apple",
                defaultUnit = ProductUnit.KG
            })
        };
        buyerRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        var buyer = await client.SendAsync(buyerRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, buyer.StatusCode);
    }
}
