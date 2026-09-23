using Haggly.Api.Authorization;
using Haggly.Api.Responses;
using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Common;
using Haggly.Application.Modules.Identity.Administration.Commands;
using Haggly.Application.Modules.Identity.Administration.Queries;
using Haggly.Application.Modules.Identity.Dtos;
using Haggly.Domain.Modules.Identity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Haggly.Api.Endpoints.Identity;

public static class VendorAdminEndpointExtensions
{
    public static IEndpointRouteBuilder MapVendorAdminEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(VendorAdminRoutes.Prefix)
            .WithTags("Vendor administration");

        group.MapGet(string.Empty, GetVendorsAsync)
            .Produces<ApiResponse<PagedResult<VendorQueryDto>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(IdentityPolicies.AdminOnly);

        group.MapPost(VendorAdminRoutes.Approve, ApproveVendorAsync)
            .Produces<ApiResponse<VendorQueryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(IdentityPolicies.AdminOnly);

        group.MapPost(VendorAdminRoutes.Reject, RejectVendorAsync)
            .Produces<ApiResponse<VendorQueryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(IdentityPolicies.AdminOnly);

        group.MapPost(VendorAdminRoutes.Suspend, SuspendVendorAsync)
            .Produces<ApiResponse<VendorQueryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(IdentityPolicies.AdminOnly);

        return endpoints;
    }

    private static async Task<IResult> GetVendorsAsync(
        [FromQuery] ApprovalStatus approvalStatus,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetVendorsQuery(approvalStatus, search, page ?? 1, pageSize ?? 20),
            cancellationToken);

        return Results.Ok(
            ApiResponse<PagedResult<VendorQueryDto>>.Create(
                result,
                "Vendors retrieved successfully."));
    }

    private static async Task<IResult> ApproveVendorAsync(
        Guid vendorId,
        [FromServices] IUserContext userContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ApproveVendorCommand(vendorId, userContext.UserId),
            cancellationToken);

        return Results.Ok(
            ApiResponse<VendorQueryDto>.Create(result, "Vendor approved successfully."));
    }

    private static async Task<IResult> RejectVendorAsync(
        Guid vendorId,
        [FromServices] IUserContext userContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RejectVendorCommand(vendorId, userContext.UserId),
            cancellationToken);

        return Results.Ok(
            ApiResponse<VendorQueryDto>.Create(result, "Vendor rejected successfully."));
    }

    private static async Task<IResult> SuspendVendorAsync(
        Guid vendorId,
        [FromServices] IUserContext userContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SuspendVendorCommand(vendorId, userContext.UserId),
            cancellationToken);

        return Results.Ok(
            ApiResponse<VendorQueryDto>.Create(result, "Vendor suspended successfully."));
    }

}
