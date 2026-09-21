using Haggly.Api.Responses;
using Haggly.Application.Modules.Discovery.Dtos;
using Haggly.Application.Modules.Discovery.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Haggly.Api.Endpoints.Discovery;

public static class MarketplaceSearchEndpointExtensions
{
    public static IEndpointRouteBuilder MapMarketplaceSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(MarketplaceSearchRoutes.Search, SearchAsync)
            .AllowAnonymous()
            .WithTags("Marketplace Search")
            .Produces<ApiResponse<MarketplaceSearchResult>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> SearchAsync(
        [FromQuery] string? q,
        [FromQuery] int? stallPage,
        [FromQuery] int? stallPageSize,
        [FromQuery] int? productPage,
        [FromQuery] int? productPageSize,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
        => Results.Ok(ApiResponse<MarketplaceSearchResult>.Create(
            await sender.Send(
                new SearchMarketplaceQuery(
                    q ?? string.Empty,
                    stallPage ?? 1,
                    stallPageSize ?? 5,
                    productPage ?? 1,
                    productPageSize ?? 20),
                cancellationToken),
            "Marketplace search completed."));
}
