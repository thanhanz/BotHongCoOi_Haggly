using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Markets.Requests;
using Haggly.Api.Responses;
using Haggly.Application.Modules.Markets.Commands;
using Haggly.Application.Modules.Markets.Dtos;
using Haggly.Application.Modules.Markets.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Haggly.Api.Endpoints.Markets;

public static class MarketEndpointExtensions
{
    public static IEndpointRouteBuilder MapMarketEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(MarketRoutes.Prefix)
            .WithTags("Markets")
            .RequireAuthorization(IdentityPolicies.AdminOnly);

        group.MapPost(string.Empty, CreateMarketAsync)
            .Produces<ApiResponse<MarketDto>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, GetMarketsAsync)
            .Produces<ApiResponse<IReadOnlyCollection<MarketDto>>>(StatusCodes.Status200OK);

        group.MapGet(MarketRoutes.ById, GetMarketByIdAsync)
            .Produces<ApiResponse<MarketDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut(MarketRoutes.ById, UpdateMarketAsync)
            .Produces<ApiResponse<MarketDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete(MarketRoutes.ById, DeleteMarketAsync)
            .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateMarketAsync(
        CreateMarketRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateMarketCommand(
                request.Code,
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.OpeningTime,
                request.ClosingTime),
            cancellationToken);

        return Results.Created(
            $"{MarketRoutes.Prefix}/{result.Id}",
            ApiResponse<MarketDto>.Create(result, "Market created successfully."));
    }

    private static async Task<IResult> GetMarketsAsync(
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMarketsQuery(), cancellationToken);

        return Results.Ok(
            ApiResponse<IReadOnlyCollection<MarketDto>>.Create(
                result,
                "Markets retrieved successfully."));
    }

    private static async Task<IResult> GetMarketByIdAsync(
        Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMarketByIdQuery(id), cancellationToken);

        return Results.Ok(
            ApiResponse<MarketDto>.Create(
                result,
                "Market retrieved successfully."));
    }

    private static async Task<IResult> UpdateMarketAsync(
        Guid id,
        UpdateMarketRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateMarketCommand(
                id,
                request.Code,
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.OpeningTime,
                request.ClosingTime,
                request.Status),
            cancellationToken);

        return Results.Ok(
            ApiResponse<MarketDto>.Create(result, "Market updated successfully."));
    }

    private static async Task<IResult> DeleteMarketAsync(
        Guid id,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteMarketCommand(id), cancellationToken);

        return Results.Ok(
            ApiResponse<bool>.Create(result, "Market deleted successfully."));
    }
}
