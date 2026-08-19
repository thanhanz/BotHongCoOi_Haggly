using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Sales;

public sealed class OrderEndpointContractTests
{
    [Theory]
    [InlineData("POST", OrderRoutes.Root)]
    [InlineData("GET", OrderRoutes.Root)]
    [InlineData("GET", OrderRoutes.Detail)]
    [InlineData("POST", OrderRoutes.Cancel)]
    public void MapOrderEndpoints_Route_IsRegisteredAndBuyerOnly(string method, string suffix)
    {
        using var app = CreateApp();
        app.MapOrderEndpoints();

        var expectedRoute = (OrderRoutes.Prefix + suffix).TrimEnd('/');
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(route => route.RoutePattern.RawText?.TrimEnd('/') == expectedRoute
                && route.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains(method));

        Assert.Contains(endpoint.Metadata.OfType<IAuthorizeData>(),
            data => data.Policy == IdentityPolicies.BuyerOnly);
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        return builder.Build();
    }
}
