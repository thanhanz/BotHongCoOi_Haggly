using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Catalog.Requests;
using Haggly.Api.Responses;
using Haggly.Application.Common;
using Haggly.Application.Modules.Catalog.Commands.ProductStalls;
using Haggly.Application.Modules.Catalog.Dtos.ProductStalls;
using Haggly.Application.Modules.Catalog.Queries.ProductStalls;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Haggly.Api.Endpoints.Catalog;

public static class ProductStallEndpointExtensions
{
    public static IEndpointRouteBuilder MapProductStallEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(ProductStallRoutes.Prefix).WithTags("Stall Products").RequireAuthorization();
            group.MapGet(string.Empty, GetPageAsync).AllowAnonymous().Produces<ApiResponse<PagedResult<ProductStallDto>>>();
            group.MapGet(ProductStallRoutes.ById, GetByIdAsync).AllowAnonymous().Produces<ApiResponse<ProductStallDto>>();
            
            group.MapPost(string.Empty, CreateAsync).RequireAuthorization(IdentityPolicies.VendorOnly)
                .Produces<ApiResponse<ProductStallDto>>(StatusCodes.Status201Created);
            group.MapPatch(ProductStallRoutes.ById, UpdateAsync).RequireAuthorization(IdentityPolicies.VendorOnly)
                .Produces<ApiResponse<ProductStallDto>>();
        return endpoints;
    }

    private static async Task<IResult> GetPageAsync(Guid stallId, int? page, int? pageSize, ISender sender, CancellationToken ct)
        => Results.Ok(ApiResponse<PagedResult<ProductStallDto>>.Create(await sender.Send(
            new GetProductStallsQuery(stallId, page ?? 1, pageSize ?? 20), ct), "Products retrieved successfully."));

    private static async Task<IResult> GetByIdAsync(Guid stallId, Guid id, ISender sender, CancellationToken ct)
        => Results.Ok(ApiResponse<ProductStallDto>.Create(await sender.Send(new GetProductStallByIdQuery(stallId, id), ct), "Product retrieved successfully."));

    private static async Task<IResult> CreateAsync(Guid stallId, CreateProductStallRequest request, HttpContext context, ISender sender, CancellationToken ct)
    {
        var actor = CurrentUserId(context);
        var result = await sender.Send(new CreateProductStallCommand(stallId, request.ProductId, actor, request.DisplayName,
            request.SellingUnit, request.MinimumOrderQuantity, request.CurrentUnitPrice, request.IsNegotiable), ct);
        return Results.Created($"{ProductStallRoutes.Prefix.Replace("{stallId:guid}", stallId.ToString())}/{result.Id}",
            ApiResponse<ProductStallDto>.Create(result, "Product added to stall successfully."));
    }

    private static async Task<IResult> UpdateAsync(Guid stallId, Guid id, UpdateProductStallRequest request, HttpContext context, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateProductStallCommand(stallId, id, CurrentUserId(context), request.DisplayName,
            request.SellingUnit, request.MinimumOrderQuantity, request.CurrentUnitPrice, request.IsNegotiable,
            request.IsActive, request.ExpectedVersion), ct);
        return Results.Ok(ApiResponse<ProductStallDto>.Create(result, "Stall product updated successfully."));
    }

    private static Guid CurrentUserId(HttpContext context)
        => Guid.TryParse(context.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
