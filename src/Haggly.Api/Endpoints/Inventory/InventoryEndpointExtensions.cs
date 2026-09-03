using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Inventory.Requests;
using Haggly.Api.Responses;
using Haggly.Application.Common;
using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Inventory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Haggly.Api.Endpoints.Inventory;

public static class InventoryEndpointExtensions
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(InventoryRoutes.Prefix)
            .WithTags("Inventory").RequireAuthorization(IdentityPolicies.VendorOnly);
            group.MapGet(InventoryRoutes.Root, GetInventoryAsync).Produces<ApiResponse<InventoryDto>>();
            group.MapPost(InventoryRoutes.Items, AddItemAsync).Produces<ApiResponse<InventoryItemDto>>(201);
            group.MapGet(InventoryRoutes.ItemById, GetItemAsync).Produces<ApiResponse<InventoryItemDto>>();
            group.MapPost(InventoryRoutes.Adjustments, AdjustInventoryAsync).Produces<ApiResponse<InventoryItemDto>>();
            group.MapGet(InventoryRoutes.Ledger, GetLedgerAsync).Produces<ApiResponse<PagedResult<InventoryLedgerDto>>>();
        return endpoints;
    }

    private static async Task<IResult> GetInventoryAsync(
            Guid stallId, [FromServices] IUserContext userContext,
            [FromServices] ISender sender, 
            CancellationToken cancellationToken)
        => Results.Ok(ApiResponse<InventoryDto>.Create(
            await sender.Send(new GetInventoryQuery(stallId, userContext.UserId), cancellationToken),
            "Inventory retrieved successfully."));

    private static async Task<IResult> AddItemAsync(
            Guid stallId, 
            AddInventoryItemRequest request,
            [FromServices] IUserContext userContext,
            [FromServices] ISender sender, 
            CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddInventoryItemCommand(
            stallId, userContext.UserId, request.ProductStallId, request.CurrentQuantity), cancellationToken);
        return Results.Created($"/api/v1/vendor/stalls/{stallId}/inventory/items/{result.Id}",
            ApiResponse<InventoryItemDto>.Create(result, "Inventory item added successfully."));
    }

    private static async Task<IResult> GetItemAsync(
            Guid stallId, 
            Guid inventoryItemId,
            [FromServices] IUserContext userContext,
            [FromServices] ISender sender, 
            CancellationToken cancellationToken)
        => Results.Ok(ApiResponse<InventoryItemDto>.Create(
            await sender.Send(new GetInventoryItemQuery(stallId, inventoryItemId, userContext.UserId), cancellationToken),
            "Inventory item retrieved successfully."));

    private static async Task<IResult> AdjustInventoryAsync(
            Guid stallId, 
            AdjustInventoryRequest request,
            [FromServices] IUserContext userContext,
            [FromServices] ISender sender, 
            CancellationToken cancellationToken)
        => Results.Ok(ApiResponse<InventoryItemDto>.Create(
            await sender.Send(new AdjustInventoryCommand(stallId, request.InventoryItemId,
                userContext.UserId, request.QuantityDelta, request.Reason, request.ExpectedVersion), cancellationToken),
            "Inventory adjusted successfully."));

    private static async Task<IResult> GetLedgerAsync(
            Guid stallId,
            [FromQuery] Guid? inventoryItemId, 
            [FromQuery] InventoryTransactionType? transactionType,
            [FromQuery] int? page, 
            [FromQuery] int? pageSize, 
            [FromServices] IUserContext userContext,
            [FromServices] ISender sender, CancellationToken cancellationToken)
        => Results.Ok(ApiResponse<PagedResult<InventoryLedgerDto>>.Create(
            await sender.Send(new GetInventoryLedgerQuery(stallId, userContext.UserId, inventoryItemId,
                transactionType, page ?? 1, pageSize ?? 20), cancellationToken),
            "Inventory ledger retrieved successfully."));

}
