using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Markets.Requests;
using Haggly.Api.Responses;
using Haggly.Application.Modules.Markets.Commands.Stalls;
using Haggly.Application.Modules.Markets.Dtos.Stalls;
using Haggly.Application.Modules.Markets.Queries.Stalls;
using Haggly.Application.Abstractions.Identity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Haggly.Api.Endpoints.Markets;

public static class StallEndpointExtensions
{
    public static IEndpointRouteBuilder MapStallEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(StallRoutes.Prefix)
            .WithTags("Stalls")
            .RequireAuthorization(IdentityPolicies.AdminOnly);

        group.MapPost(string.Empty, CreateStallAsync)
            .Produces<ApiResponse<StallDto>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, GetStallsAsync)
            .Produces<ApiResponse<IReadOnlyCollection<StallDto>>>(StatusCodes.Status200OK);

        group.MapGet(StallRoutes.ById, GetStallByIdAsync)
            .Produces<ApiResponse<StallDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut(StallRoutes.ById, UpdateStallAsync)
            .Produces<ApiResponse<StallDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete(StallRoutes.ById, DeleteStallAsync)
            .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateStallAsync(
        CreateStallRequest request,
        [FromServices] IUserContext userContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateStallCommand(
                request.MarketId,
                request.VendorId,
                userContext.UserId,
                request.Code,
                request.Name,
                request.LocationDescription,
                request.PhoneNumber),
            cancellationToken);

        return Results.Created(
            $"{StallRoutes.Prefix}/{result.Id}",
            ApiResponse<StallDto>.Create(result, "Stall created successfully."));
    }

    private static async Task<IResult> GetStallsAsync(
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStallsQuery(), cancellationToken);

        return Results.Ok(
            ApiResponse<IReadOnlyCollection<StallDto>>.Create(
                result,
                "Stalls retrieved successfully."));
    }

    private static async Task<IResult> GetStallByIdAsync(
        Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStallByIdQuery(id), cancellationToken);

        return Results.Ok(
            ApiResponse<StallDto>.Create(result, "Stall retrieved successfully."));
    }

    private static async Task<IResult> UpdateStallAsync(
        Guid id,
        UpdateStallRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateStallCommand(
                id,
                request.MarketId,
                request.VendorId,
                request.Code,
                request.Name,
                request.LocationDescription,
                request.PhoneNumber,
                request.Status),
            cancellationToken);

        return Results.Ok(
            ApiResponse<StallDto>.Create(result, "Stall updated successfully."));
    }

    private static async Task<IResult> DeleteStallAsync(
        Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteStallCommand(id), cancellationToken);

        return Results.Ok(
            ApiResponse<bool>.Create(result, "Stall deleted successfully."));
    }
}
