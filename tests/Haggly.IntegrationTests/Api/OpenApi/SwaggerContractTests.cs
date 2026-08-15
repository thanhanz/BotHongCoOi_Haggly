using System.Net;
using Haggly.Api;
using Haggly.Api.Endpoints.Identity;
using Haggly.Api.Endpoints.Inventory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Haggly.IntegrationTests.Api.OpenApi;

public sealed class SwaggerContractTests
{
    [Fact]
    public async Task SwaggerDocument_WhenDevelopmentPipelineIsConfigured_RedirectsRootAndServesJson()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddApiServices();
        await using var app = builder.Build();

        app.UseSwaggerDocumentation();
        app.MapIdentityEndpoints();
        app.MapInventoryEndpoints();

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(address)
        };

        var root = await client.GetAsync("/");
        var document = await client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.Redirect, root.StatusCode);
        Assert.Equal("/swagger", root.Headers.Location?.OriginalString);
        Assert.Contains("Haggly API", document);
        Assert.Contains("/api/v1/identity/login", document);
        Assert.Contains("inventory-sessions/open", document);
    }
}
