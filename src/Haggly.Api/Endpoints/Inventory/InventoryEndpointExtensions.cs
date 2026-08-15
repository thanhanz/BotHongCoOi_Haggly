using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Inventory.Requests;
using Haggly.Api.Responses;
using Haggly.Application.Common;
using Haggly.Application.Modules.Inventory.Commands;
using Haggly.Application.Modules.Inventory.Dtos;
using Haggly.Application.Modules.Inventory.Queries;
using Haggly.Domain.Modules.Inventory;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Haggly.Api.Endpoints.Inventory;

public static class InventoryEndpointExtensions
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(InventoryRoutes.Prefix)
            .WithTags("Inventory")
            .RequireAuthorization(IdentityPolicies.VendorOnly);

        group.MapPost(InventoryRoutes.OpenSession, OpenSessionAsync)
            .Produces<ApiResponse<InventorySessionDto>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(InventoryRoutes.CurrentSession, GetCurrentSessionAsync)
            .Produces<ApiResponse<InventorySessionDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet(InventoryRoutes.PreviousSession, GetPreviousSessionAsync)
            .Produces<ApiResponse<InventorySessionDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(InventoryRoutes.CloseSession, CloseSessionAsync)
            .Produces<ApiResponse<InventorySessionDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost(InventoryRoutes.Listings, AddListingAsync)
            .Produces<ApiResponse<DailyProductListingDto>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch(InventoryRoutes.ListingById, UpdateListingAsync)
            .Produces<ApiResponse<DailyProductListingDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost(InventoryRoutes.Adjustments, AdjustInventoryAsync)
            .Produces<ApiResponse<DailyProductListingDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(InventoryRoutes.Ledger, GetLedgerAsync)
            .Produces<ApiResponse<PagedResult<InventoryLedgerDto>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> OpenSessionAsync(
        Guid stallId,
        OpenInventorySessionRequest request,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new OpenInventorySessionCommand(
                stallId,
                CurrentUserId(context),
                request.Notes,
                request.Listings?.Select(ToListingInput).ToArray() ?? []),
            cancellationToken);

        return Results.Created(
            BuildRoute(InventoryRoutes.CurrentSession, stallId),
            ApiResponse<InventorySessionDto>.Create(
                result,
                "Inventory session opened successfully."));
    }

    private static async Task<IResult> GetCurrentSessionAsync(
        Guid stallId,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetCurrentInventorySessionQuery(stallId, CurrentUserId(context)),
            cancellationToken);

        return Results.Ok(ApiResponse<InventorySessionDto>.Create(
            result,
            "Current inventory session retrieved successfully."));
    }

    private static async Task<IResult> GetPreviousSessionAsync(
        Guid stallId,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPreviousInventorySessionQuery(stallId, CurrentUserId(context)),
            cancellationToken);

        return Results.Ok(ApiResponse<InventorySessionDto>.Create(
            result,
            "Previous inventory session retrieved successfully."));
    }

    private static async Task<IResult> CloseSessionAsync(
        Guid stallId,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CloseInventorySessionCommand(stallId, CurrentUserId(context)),
            cancellationToken);

        return Results.Ok(ApiResponse<InventorySessionDto>.Create(
            result,
            "Inventory session closed successfully."));
    }

    private static async Task<IResult> AddListingAsync(
        Guid stallId,
        InventoryListingRequest request,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddDailyProductListingCommand(
                stallId,
                CurrentUserId(context),
                ToListingInput(request)),
            cancellationToken);

        return Results.Created(
            BuildRoute(InventoryRoutes.ListingById, stallId, result.Id),
            ApiResponse<DailyProductListingDto>.Create(
                result,
                "Daily product listing added successfully."));
    }

    private static async Task<IResult> UpdateListingAsync(
        Guid stallId,
        Guid listingId,
        UpdateDailyProductListingRequest request,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateDailyProductListingCommand(
                stallId,
                listingId,
                CurrentUserId(context),
                request.PublicUnitPrice,
                request.Status,
                request.ExpectedVersion),
            cancellationToken);

        return Results.Ok(ApiResponse<DailyProductListingDto>.Create(
            result,
            "Daily product listing updated successfully."));
    }

    private static async Task<IResult> AdjustInventoryAsync(
        Guid stallId,
        AdjustInventoryRequest request,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AdjustInventoryCommand(
                stallId,
                request.ListingId,
                CurrentUserId(context),
                request.QuantityDelta,
                request.Reason,
                request.ExpectedVersion),
            cancellationToken);

        return Results.Ok(ApiResponse<DailyProductListingDto>.Create(
            result,
            "Inventory adjusted successfully."));
    }

    private static async Task<IResult> GetLedgerAsync(
        Guid stallId,
        [FromQuery] DateOnly? businessDate,
        [FromQuery] Guid? listingId,
        [FromQuery] InventoryTransactionType? transactionType,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetInventoryLedgerQuery(
                stallId,
                CurrentUserId(context),
                businessDate,
                listingId,
                transactionType,
                page ?? 1,
                pageSize ?? 20),
            cancellationToken);

        return Results.Ok(ApiResponse<PagedResult<InventoryLedgerDto>>.Create(
            result,
            "Inventory ledger retrieved successfully."));
    }

    private static InventoryListingInput ToListingInput(InventoryListingRequest request)
        => new(request.ProductStallId, request.OpeningQuantity, request.PublicUnitPrice);

    private static Guid CurrentUserId(HttpContext context)
        => Guid.TryParse(
            context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var id)
            ? id
            : Guid.Empty;

    private static string BuildRoute(string routeTemplate, Guid stallId, Guid? listingId = null)
        => routeTemplate
            .Replace("{stallId:guid}", stallId.ToString())
            .Replace("{listingId:guid}", listingId?.ToString() ?? string.Empty);
}
