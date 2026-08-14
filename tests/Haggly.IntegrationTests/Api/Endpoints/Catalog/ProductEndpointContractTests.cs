using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Catalog;
using Haggly.Api.Responses;
using Haggly.Application.Modules.Catalog.Dtos.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Catalog;

public sealed class ProductEndpointContractTests
{
    [Fact]
    public void MapProductEndpoints_WhenMapped_RegistersCreateAndReadRoutes()
    {
        using var app = CreateApp();

        app.MapProductEndpoints();

        var routes = GetRoutes(app);

        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == ProductRoutes.Prefix + "/"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["POST"]));
        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == ProductRoutes.Prefix + "/"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["GET"]));
        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == ProductRoutes.Prefix + ProductRoutes.ById
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["GET"]));
    }

    [Fact]
    public void MapProductEndpoints_WhenMapped_RequiresAuthenticationForAllRoutesAndContributorPolicyForCreate()
    {
        using var app = CreateApp();

        app.MapProductEndpoints();

        var routes = GetRoutes(app);
        Assert.All(routes, endpoint =>
            Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData));

        var create = routes.Single(endpoint =>
            endpoint.RoutePattern.RawText == ProductRoutes.Prefix + "/"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["POST"]));
        Assert.Contains(
            create.Metadata.OfType<IAuthorizeData>(),
            metadata => metadata.Policy == IdentityPolicies.CatalogContributor);
    }

    [Theory]
    [InlineData("/api/v1/products/", "POST", 201, typeof(ApiResponse<ProductDto>))]
    [InlineData("/api/v1/products/", "GET", 200, typeof(ApiResponse<IReadOnlyCollection<ProductDto>>))]
    [InlineData("/api/v1/products/{id:guid}", "GET", 200, typeof(ApiResponse<ProductDto>))]
    public void MapProductEndpoints_WhenMapped_DocumentsSuccessfulResponses(
        string route,
        string method,
        int statusCode,
        Type responseType)
    {
        using var app = CreateApp();

        app.MapProductEndpoints();

        Assert.Contains(
            GetRoutes(app),
            endpoint => endpoint.RoutePattern.RawText == route
                && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual([method])
                && endpoint.Metadata.OfType<IProducesResponseTypeMetadata>()
                    .Any(metadata => metadata.StatusCode == statusCode && metadata.Type == responseType));
    }

    [Theory]
    [InlineData("/api/v1/products/", "POST", 400)]
    [InlineData("/api/v1/products/", "POST", 401)]
    [InlineData("/api/v1/products/", "POST", 403)]
    [InlineData("/api/v1/products/", "POST", 404)]
    [InlineData("/api/v1/products/", "POST", 409)]
    [InlineData("/api/v1/products/{id:guid}", "GET", 400)]
    [InlineData("/api/v1/products/{id:guid}", "GET", 401)]
    [InlineData("/api/v1/products/{id:guid}", "GET", 404)]
    public void MapProductEndpoints_WhenMapped_DocumentsProblemDetails(
        string route,
        string method,
        int statusCode)
    {
        using var app = CreateApp();

        app.MapProductEndpoints();

        Assert.Contains(
            GetRoutes(app),
            endpoint => endpoint.RoutePattern.RawText == route
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
}
