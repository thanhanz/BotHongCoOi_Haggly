using Haggly.Api.Endpoints.Markets;
using Haggly.Api.Responses;
using Haggly.Application.Modules.Markets.Dtos;
using Haggly.Domain.Modules.Markets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.IntegrationTests;

public sealed class MarketEndpointContractTests
{
    [Fact]
    public void MapMarketEndpoints_WhenMapped_UsesMarketsV1Prefix()
    {
        Assert.Equal("/api/v1/markets", MarketRoutes.Prefix);
    }

    [Fact]
    public void MapMarketEndpoints_WhenMapped_RegistersWriteRoutes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapMarketEndpoints();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == MarketRoutes.Prefix + "/"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["POST"]));
        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == MarketRoutes.Prefix + "/"
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["GET"]));
        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == MarketRoutes.Prefix + MarketRoutes.ById
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["PUT"]));
        Assert.Contains(routes, endpoint =>
            endpoint.RoutePattern.RawText == MarketRoutes.Prefix + MarketRoutes.ById
            && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["DELETE"]));
    }

    [Fact]
    public void MapMarketEndpoints_WhenMapped_RequiresAdminAuthorization()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapMarketEndpoints();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText is "/api/v1/markets/" or "/api/v1/markets/{id:guid}")
            .ToArray();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, endpoint =>
            Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData));
    }

    [Theory]
    [InlineData("/api/v1/markets/", "POST", 201, typeof(ApiResponse<MarketDto>))]
    [InlineData("/api/v1/markets/", "GET", 200, typeof(ApiResponse<IReadOnlyCollection<MarketDto>>))]
    [InlineData("/api/v1/markets/{id:guid}", "GET", 200, typeof(ApiResponse<MarketDto>))]
    [InlineData("/api/v1/markets/{id:guid}", "PUT", 200, typeof(ApiResponse<MarketDto>))]
    [InlineData("/api/v1/markets/{id:guid}", "DELETE", 200, typeof(ApiResponse<bool>))]
    public void MapMarketEndpoints_WhenMapped_DocumentsSuccessfulResponse(
        string route,
        string method,
        int statusCode,
        Type responseType)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapMarketEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == route
                && candidate.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual([method])
                && candidate.Metadata.OfType<IProducesResponseTypeMetadata>()
                    .Any(metadata => metadata.StatusCode == statusCode && metadata.Type == responseType));

        Assert.Contains(
            endpoint.Metadata.OfType<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == statusCode && metadata.Type == responseType);
    }

    [Fact]
    public void MapMarketEndpoints_WhenMapped_DocumentsGetByIdNotFoundResponse()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapMarketEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == "/api/v1/markets/{id:guid}"
                && candidate.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.SequenceEqual(["GET"]));

        Assert.Contains(
            endpoint.Metadata.OfType<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status404NotFound);
    }
}
