using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Catalog;
using Haggly.Api.Responses;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Dtos.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Catalog;

public sealed class CategoryEndpointContractTests
{
    [Fact]
    public void MapCategoryEndpoints_WhenMapped_RegistersCreateAndReadRoutes()
    {
        using var app = CreateApp();

        app.MapCategoryEndpoints();

        var routes = GetRoutes(app);

        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == CategoryRoutes.Prefix + "/"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["POST"]));
        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == CategoryRoutes.Prefix + "/"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["GET"]));
        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == CategoryRoutes.Prefix + CategoryRoutes.ById
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["GET"]));
    }

    [Fact]
    public void MapCategoryEndpoints_WhenMapped_RequiresAuthenticationForAllRoutesAndContributorPolicyForCreate()
    {
        using var app = CreateApp();

        app.MapCategoryEndpoints();

        var routes = GetRoutes(app);
        Assert.All(routes, endpoint =>
            Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData));

        var create = routes.Single(endpoint =>
            endpoint.RoutePattern.RawText == CategoryRoutes.Prefix + "/"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["POST"]));
        Assert.Contains(
            create.Metadata.OfType<IAuthorizeData>(),
            metadata => metadata.Policy == IdentityPolicies.CatalogContributor);
    }

    [Theory]
    [InlineData("/api/v1/categories/", "POST", 201, typeof(ApiResponse<CategoryDto>))]
    [InlineData("/api/v1/categories/", "GET", 200, typeof(ApiResponse<PagedResult<CategoryDto>>))]
    [InlineData("/api/v1/categories/{id:guid}", "GET", 200, typeof(ApiResponse<CategoryDto>))]
    public void MapCategoryEndpoints_WhenMapped_DocumentsSuccessfulResponses(
        string route,
        string method,
        int statusCode,
        Type responseType)
    {
        using var app = CreateApp();

        app.MapCategoryEndpoints();

        Assert.Contains(
            GetRoutes(app),
            endpoint => endpoint.RoutePattern.RawText == route
                && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual([method])
                && endpoint.Metadata.OfType<IProducesResponseTypeMetadata>()
                    .Any(metadata => metadata.StatusCode == statusCode && metadata.Type == responseType));
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
