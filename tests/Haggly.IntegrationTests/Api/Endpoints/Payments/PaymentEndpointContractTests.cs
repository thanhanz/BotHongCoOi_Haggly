using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.IntegrationTests.Api.Endpoints.Payments;

public sealed class PaymentEndpointContractTests
{
    [Fact]
    public void MapPaymentEndpoints_StartPaymentRoute_IsRegisteredAndBuyerOnly()
    {
        using var app = CreateApp();
        app.MapPaymentEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(route => route.RoutePattern.RawText?.TrimEnd('/') == PaymentRoutes.Prefix
                && route.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains("POST"));

        Assert.Contains(endpoint.Metadata.OfType<IAuthorizeData>(),
            data => data.Policy == IdentityPolicies.BuyerOnly);
        Assert.Contains(endpoint.Metadata.OfType<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == StatusCodes.Status202Accepted);
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        return builder.Build();
    }
}
