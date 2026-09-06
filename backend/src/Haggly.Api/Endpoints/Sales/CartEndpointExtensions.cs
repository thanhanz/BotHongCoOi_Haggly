using Haggly.Api.Authorization;
using Haggly.Application.Abstractions.Identity;
using Haggly.Api.Endpoints.Sales.Requests;
using Haggly.Api.Responses;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Haggly.Api.Endpoints.Sales;

public static class CartEndpointExtensions
{
    public static IEndpointRouteBuilder MapCartEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(CartRoutes.Prefix)
            .WithTags("Cart")
            .RequireAuthorization(IdentityPolicies.BuyerOnly);

        group.MapGet(CartRoutes.Root, GetAsync)
            .Produces<ApiResponse<CartDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost(CartRoutes.Items, AddItemAsync)
            .Produces<ApiResponse<CartDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut(CartRoutes.ItemById, UpdateItemAsync)
            .Produces<ApiResponse<CartDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete(CartRoutes.ItemById, RemoveItemAsync)
            .Produces<ApiResponse<CartDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete(CartRoutes.Root, ClearAsync)
            .Produces<ApiResponse<CartDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost(CartRoutes.Checkout, CheckoutAsync)
            .Produces<ApiResponse<OrderDto>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        [FromServices] IUserContext userContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
        => Results.Ok(ApiResponse<CartDto>.Create(
            await sender.Send(new GetCartQuery(userContext.UserId), cancellationToken),
            "Cart retrieved successfully."));

    private static async Task<IResult> AddItemAsync(
        AddCartItemRequest request,
        [FromServices] IUserContext userContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
        => Results.Ok(ApiResponse<CartDto>.Create(
            await sender.Send(
                new AddCartItemCommand(
                    userContext.UserId,
                    request.InventoryItemId,
                    request.Quantity,
                    request.Notes),
                cancellationToken),
            "Cart item added successfully."));

    private static async Task<IResult> UpdateItemAsync(
        Guid cartItemId,
        UpdateCartItemRequest request,
        [FromServices] IUserContext userContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
        => Results.Ok(ApiResponse<CartDto>.Create(
            await sender.Send(
                new UpdateCartItemCommand(
                    userContext.UserId,
                    cartItemId,
                    request.Quantity,
                    request.Notes),
                cancellationToken),
            "Cart item updated successfully."));

    private static async Task<IResult> RemoveItemAsync(
        Guid cartItemId,
        [FromServices] IUserContext userContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
        => Results.Ok(ApiResponse<CartDto>.Create(
            await sender.Send(
                new RemoveCartItemCommand(userContext.UserId, cartItemId),
                cancellationToken),
            "Cart item removed successfully."));

    private static async Task<IResult> ClearAsync(
        [FromServices] IUserContext userContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
        => Results.Ok(ApiResponse<CartDto>.Create(
            await sender.Send(
                new ClearCartCommand(userContext.UserId),
                cancellationToken),
            "Cart cleared successfully."));

    private static async Task<IResult> CheckoutAsync(
        [FromServices] IUserContext userContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CheckoutCartCommand(userContext.UserId),
            cancellationToken);

        return Results.Created(
            $"{OrderRoutes.Prefix}/{result.Id}",
            ApiResponse<OrderDto>.Create(result, "Cart checked out successfully."));
    }

}
