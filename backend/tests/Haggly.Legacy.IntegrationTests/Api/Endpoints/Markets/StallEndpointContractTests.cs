using Haggly.Api.Endpoints.Markets;
using Haggly.Api.Responses;
using Haggly.Application.Modules.Markets.Dtos.Stalls;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Markets;

public sealed class StallEndpointContractTests
{
    [Fact]
    public void MapStallEndpoints_WhenMapped_RegistersCrudRoutes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapStallEndpoints();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == StallRoutes.Prefix + "/"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["POST"]));
        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == StallRoutes.Prefix + "/"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["GET"]));
        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == StallRoutes.Prefix + StallRoutes.ById
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["GET"]));
        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == StallRoutes.Prefix + StallRoutes.ById
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["PUT"]));
        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == StallRoutes.Prefix + StallRoutes.ById
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["DELETE"]));
    }

    [Fact]
    public void MapStallEndpoints_WhenMapped_RequiresAdminAuthorization()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapStallEndpoints();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, endpoint =>
            Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData));
    }

    [Theory]
    [InlineData("/api/v1/markets/stalls/", "POST", 201, typeof(ApiResponse<StallDto>))]
    [InlineData("/api/v1/markets/stalls/", "GET", 200, typeof(ApiResponse<IReadOnlyCollection<StallDto>>))]
    [InlineData("/api/v1/markets/stalls/{id:guid}", "GET", 200, typeof(ApiResponse<StallDto>))]
    [InlineData("/api/v1/markets/stalls/{id:guid}", "PUT", 200, typeof(ApiResponse<StallDto>))]
    [InlineData("/api/v1/markets/stalls/{id:guid}", "DELETE", 200, typeof(ApiResponse<bool>))]
    public void MapStallEndpoints_WhenMapped_DocumentsSuccessfulResponse(
        string route,
        string method,
        int statusCode,
        Type responseType)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapStallEndpoints();

        Assert.Contains(
            ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>(),
            endpoint => endpoint.RoutePattern.RawText == route
                && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual([method])
                && endpoint.Metadata.OfType<IProducesResponseTypeMetadata>()
                    .Any(metadata => metadata.StatusCode == statusCode && metadata.Type == responseType));
    }
}
