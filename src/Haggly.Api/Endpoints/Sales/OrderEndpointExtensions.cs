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

public static class OrderEndpointExtensions
{
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(OrderRoutes.Prefix)
            .WithTags("Orders")
            .RequireAuthorization(IdentityPolicies.BuyerOnly);

        group.MapPost(OrderRoutes.Root, CreateAsync)
            .Produces<ApiResponse<OrderDto>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(OrderRoutes.Root, GetPageAsync)
            .Produces<ApiResponse<PagedResult<OrderDto>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet(OrderRoutes.Detail, GetDetailsAsync)
            .Produces<ApiResponse<OrderDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(OrderRoutes.Cancel, CancelAsync)
            .Produces<ApiResponse<OrderDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateOrderRequest request,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateOrderCommand(
                CurrentUserId(context),
                request.Items?.Select(item => new CreateOrderLine(
                    item.InventoryItemId,
                    item.Quantity,
                    item.Notes)).ToArray() ?? []),
            cancellationToken);

        return Results.Created(
            $"{OrderRoutes.Prefix}/{result.Id}",
            ApiResponse<OrderDto>.Create(result, "Order created successfully."));
    }

    private static async Task<IResult> GetPageAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOrdersQuery(CurrentUserId(context), page ?? 1, pageSize ?? 20),
            cancellationToken);

        return Results.Ok(ApiResponse<PagedResult<OrderDto>>.Create(
            result,
            "Orders retrieved successfully."));
    }

    private static async Task<IResult> GetDetailsAsync(
        Guid orderId,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOrderDetailsQuery(orderId, CurrentUserId(context)),
            cancellationToken);

        return Results.Ok(ApiResponse<OrderDto>.Create(
            result,
            "Order details retrieved successfully."));
    }

    private static async Task<IResult> CancelAsync(
        Guid orderId,
        CancelOrderRequest request,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CancelOrderCommand(orderId, CurrentUserId(context), request.Reason),
            cancellationToken);

        return Results.Ok(ApiResponse<OrderDto>.Create(
            result,
            "Order cancelled successfully."));
    }

    private static Guid CurrentUserId(HttpContext context)
        => Guid.TryParse(
            context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var id)
            ? id
            : Guid.Empty;
}
