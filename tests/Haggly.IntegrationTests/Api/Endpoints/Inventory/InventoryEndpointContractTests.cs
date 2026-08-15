using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Inventory;
using Haggly.Api.Responses;
using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Domain.Modules.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Inventory;

public sealed class InventoryEndpointContractTests
{
    [Fact]
    public void MapInventoryEndpoints_WhenMapped_RegistersAllInventoryRoutes()
    {
        using var app = CreateApp();

        app.MapInventoryEndpoints();

        var routes = GetRoutes(app);
        Assert.Contains(routes, endpoint => IsRoute(InventoryRoutes.Prefix + InventoryRoutes.OpenSession, "POST")(endpoint));
        Assert.Contains(routes, endpoint => IsRoute(InventoryRoutes.Prefix + InventoryRoutes.CurrentSession, "GET")(endpoint));
        Assert.Contains(routes, endpoint => IsRoute(InventoryRoutes.Prefix + InventoryRoutes.PreviousSession, "GET")(endpoint));
        Assert.Contains(routes, endpoint => IsRoute(InventoryRoutes.Prefix + InventoryRoutes.CloseSession, "POST")(endpoint));
        Assert.Contains(routes, endpoint => IsRoute(InventoryRoutes.Prefix + InventoryRoutes.Listings, "POST")(endpoint));
        Assert.Contains(routes, endpoint => IsRoute(InventoryRoutes.Prefix + InventoryRoutes.ListingById, "PATCH")(endpoint));
        Assert.Contains(routes, endpoint => IsRoute(InventoryRoutes.Prefix + InventoryRoutes.Adjustments, "POST")(endpoint));
        Assert.Contains(routes, endpoint => IsRoute(InventoryRoutes.Prefix + InventoryRoutes.Ledger, "GET")(endpoint));
    }

    [Fact]
    public void MapInventoryEndpoints_WhenMapped_RequiresVendorOnlyAuthorizationForEveryRoute()
    {
        using var app = CreateApp();

        app.MapInventoryEndpoints();

        Assert.NotEmpty(GetRoutes(app));
        Assert.All(GetRoutes(app), endpoint =>
            Assert.Contains(
                endpoint.Metadata.OfType<IAuthorizeData>(),
                metadata => metadata.Policy == IdentityPolicies.VendorOnly));
    }

    [Theory]
    [InlineData("POST", InventoryRoutes.OpenSession, 201, typeof(ApiResponse<InventorySessionDto>))]
    [InlineData("GET", InventoryRoutes.CurrentSession, 200, typeof(ApiResponse<InventorySessionDto>))]
    [InlineData("GET", InventoryRoutes.PreviousSession, 200, typeof(ApiResponse<InventorySessionDto>))]
    [InlineData("POST", InventoryRoutes.CloseSession, 200, typeof(ApiResponse<InventorySessionDto>))]
    [InlineData("POST", InventoryRoutes.Listings, 201, typeof(ApiResponse<DailyProductListingDto>))]
    [InlineData("PATCH", InventoryRoutes.ListingById, 200, typeof(ApiResponse<DailyProductListingDto>))]
    [InlineData("POST", InventoryRoutes.Adjustments, 200, typeof(ApiResponse<DailyProductListingDto>))]
    [InlineData("GET", InventoryRoutes.Ledger, 200, typeof(ApiResponse<PagedResult<InventoryLedgerDto>>))]
    public void MapInventoryEndpoints_WhenMapped_DocumentsSuccessfulResponse(
        string method,
        string routeSuffix,
        int statusCode,
        Type responseType)
    {
        using var app = CreateApp();

        app.MapInventoryEndpoints();

        Assert.Contains(
            GetRoutes(app),
            endpoint => endpoint.RoutePattern.RawText == InventoryRoutes.Prefix + routeSuffix
                && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual([method])
                && endpoint.Metadata.OfType<IProducesResponseTypeMetadata>()
                    .Any(metadata => metadata.StatusCode == statusCode && metadata.Type == responseType));
    }

    [Theory]
    [InlineData("POST", InventoryRoutes.OpenSession, 400)]
    [InlineData("POST", InventoryRoutes.OpenSession, 401)]
    [InlineData("POST", InventoryRoutes.OpenSession, 403)]
    [InlineData("POST", InventoryRoutes.OpenSession, 404)]
    [InlineData("POST", InventoryRoutes.OpenSession, 409)]
    [InlineData("GET", InventoryRoutes.CurrentSession, 401)]
    [InlineData("GET", InventoryRoutes.CurrentSession, 403)]
    [InlineData("GET", InventoryRoutes.CurrentSession, 404)]
    [InlineData("POST", InventoryRoutes.Adjustments, 400)]
    [InlineData("POST", InventoryRoutes.Adjustments, 409)]
    public void MapInventoryEndpoints_WhenMapped_DocumentsProblemDetails(
        string method,
        string routeSuffix,
        int statusCode)
    {
        using var app = CreateApp();

        app.MapInventoryEndpoints();

        Assert.Contains(
            GetRoutes(app),
            endpoint => endpoint.RoutePattern.RawText == InventoryRoutes.Prefix + routeSuffix
                && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual([method])
                && endpoint.Metadata.OfType<IProducesResponseTypeMetadata>()
                    .Any(metadata => metadata.StatusCode == statusCode));
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        return builder.Build();
    }

    private static RouteEndpoint[] GetRoutes(WebApplication app)
        => ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

    private static Func<RouteEndpoint, bool> IsRoute(string route, string method)
        => endpoint => endpoint.RoutePattern.RawText == route
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual([method]);
}
