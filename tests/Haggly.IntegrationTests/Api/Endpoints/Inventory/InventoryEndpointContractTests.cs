using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Inventory;

public sealed class InventoryEndpointContractTests
{
    [Theory]
    [InlineData("GET", InventoryRoutes.Root)]
    [InlineData("POST", InventoryRoutes.Items)]
    [InlineData("GET", InventoryRoutes.ItemById)]
    [InlineData("POST", InventoryRoutes.Adjustments)]
    [InlineData("GET", InventoryRoutes.Ledger)]
    public void MapInventoryEndpoints_ContinuousRoute_IsRegisteredAndVendorOnly(string method, string suffix)
    {
        using var app = CreateApp();
        app.MapInventoryEndpoints();
        var endpoint = GetRoutes(app).Single(route => route.RoutePattern.RawText == InventoryRoutes.Prefix + suffix
            && route.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains(method));
        Assert.Contains(endpoint.Metadata.OfType<IAuthorizeData>(), data => data.Policy == IdentityPolicies.VendorOnly);
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        return builder.Build();
    }

    private static RouteEndpoint[] GetRoutes(WebApplication app)
        => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>().ToArray();
}
