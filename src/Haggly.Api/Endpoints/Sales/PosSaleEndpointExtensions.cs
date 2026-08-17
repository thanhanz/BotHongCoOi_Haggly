using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Sales.Requests;
using Haggly.Api.Responses;
using Haggly.Application.Common;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Haggly.Api.Endpoints.Sales;

public static class PosSaleEndpointExtensions
{
    public static IEndpointRouteBuilder MapPosSaleEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PosSaleRoutes.Prefix)
            .WithTags("Vendor POS")
            .RequireAuthorization(IdentityPolicies.VendorOnly);

        group.MapPost(PosSaleRoutes.Root, CompleteAsync)
            .Produces<ApiResponse<PosSaleDto>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(PosSaleRoutes.Root, GetHistoryAsync)
            .Produces<ApiResponse<PagedResult<PosSaleDto>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CompleteAsync(
        Guid stallId,
        CompletePosSaleRequest request,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CompletePosSaleCommand(
                stallId,
                CurrentUserId(context),
                request.ClientRequestId,
                request.Items.Select(item => new PosSaleLineInput(
                    item.InventoryItemId,
                    item.Quantity,
                    item.ExpectedInventoryVersion,
                    item.ExpectedProductStallVersion)).ToArray(),
                request.PaymentMethod,
                request.AmountPaid),
            cancellationToken);

        return Results.Created(
            $"{PosSaleRoutes.Prefix.Replace("{stallId:guid}", stallId.ToString())}/{result.Id}",
            ApiResponse<PosSaleDto>.Create(result, "POS sale completed successfully."));
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid stallId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPosSalesQuery(
                stallId,
                CurrentUserId(context),
                page ?? 1,
                pageSize ?? 20),
            cancellationToken);

        return Results.Ok(ApiResponse<PagedResult<PosSaleDto>>.Create(
            result,
            "POS sales retrieved successfully."));
    }

    private static Guid CurrentUserId(HttpContext context)
        => Guid.TryParse(
            context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var id)
            ? id
            : Guid.Empty;
}
