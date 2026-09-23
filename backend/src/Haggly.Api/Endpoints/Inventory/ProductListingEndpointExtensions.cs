using Haggly.Api.Responses;
using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Haggly.Api.Endpoints.Inventory;

public static class ProductListingEndpointExtensions
{
    public static IEndpointRouteBuilder MapProductListingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(ProductListingRoutes.Prefix, GetPageAsync)
            .AllowAnonymous()
            .WithTags("Product Listings")
            .Produces<ApiResponse<PagedResult<ProductListingDto>>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> GetPageAsync(
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? stallId,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
        => Results.Ok(ApiResponse<PagedResult<ProductListingDto>>.Create(
            await sender.Send(new GetProductListingsQuery(
                categoryId, stallId, sort, page ?? 1, pageSize ?? 10), cancellationToken),
            "Product listings retrieved successfully."));
}
