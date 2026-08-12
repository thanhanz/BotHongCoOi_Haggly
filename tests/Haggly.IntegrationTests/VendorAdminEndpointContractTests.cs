using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Identity;
using Haggly.Api.Responses;
using Haggly.Application.Common;
using Haggly.Application.Modules.Identity.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.IntegrationTests;

public sealed class VendorAdminEndpointContractTests
{
    [Fact]
    public void MapVendorAdminEndpoints_RegistersAdminVendorListRoute()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapVendorAdminEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == "/api/v1/admin/vendors/");

        Assert.Equal(["GET"], endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
        Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData);
    }

    [Fact]
    public void MapVendorAdminEndpoints_DocumentsPagedResponseAndValidationFailure()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapVendorAdminEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == "/api/v1/admin/vendors/");

        Assert.Contains(
            endpoint.Metadata.OfType<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status200OK
                && metadata.Type == typeof(ApiResponse<PagedResult<VendorAdminDto>>));
        Assert.Contains(
            endpoint.Metadata.OfType<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status400BadRequest
                && metadata.Type == typeof(ProblemDetails));
    }

    [Theory]
    [InlineData("/api/v1/admin/vendors/{vendorId:guid}/approve")]
    [InlineData("/api/v1/admin/vendors/{vendorId:guid}/reject")]
    [InlineData("/api/v1/admin/vendors/{vendorId:guid}/suspend")]
    public void MapVendorAdminEndpoints_RegistersActionRoutesWithAdminAuthorization(
        string route)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        using var app = builder.Build();

        app.MapVendorAdminEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == route);

        Assert.Equal(["POST"], endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
        Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData);
        Assert.Contains(
            endpoint.Metadata.OfType<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status200OK
                && metadata.Type == typeof(ApiResponse<VendorAdminDto>));
    }
}
