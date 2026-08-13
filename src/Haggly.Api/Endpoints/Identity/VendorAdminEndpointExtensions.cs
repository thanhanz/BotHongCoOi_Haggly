using Haggly.Api.Authorization;
using Haggly.Api.Responses;
using Haggly.Application.Common;
using Haggly.Application.Modules.Identity.Administration.Commands;
using Haggly.Application.Modules.Identity.Administration.Queries;
using Haggly.Application.Modules.Identity.Dtos;
using Haggly.Domain.Modules.Identity;
using MediatR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        HttpContext httpContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetSubject(httpContext, out var adminId, out var problem))
            return problem!;

        var result = await sender.Send(
            new ApproveVendorCommand(vendorId, adminId),
            cancellationToken);

        return Results.Ok(
            ApiResponse<VendorQueryDto>.Create(result, "Vendor approved successfully."));
    }

    private static async Task<IResult> RejectVendorAsync(
        Guid vendorId,
        HttpContext httpContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetSubject(httpContext, out var adminId, out var problem))
            return problem!;

        var result = await sender.Send(
            new RejectVendorCommand(vendorId, adminId),
            cancellationToken);

        return Results.Ok(
            ApiResponse<VendorQueryDto>.Create(result, "Vendor rejected successfully."));
    }

    private static async Task<IResult> SuspendVendorAsync(
        Guid vendorId,
        HttpContext httpContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        if (!TryGetSubject(httpContext, out var adminId, out var problem))
            return problem!;

        var result = await sender.Send(
            new SuspendVendorCommand(vendorId, adminId),
            cancellationToken);

        return Results.Ok(
            ApiResponse<VendorQueryDto>.Create(result, "Vendor suspended successfully."));
    }

    private static bool TryGetSubject(
        HttpContext httpContext,
        out Guid userId,
        out IResult? problem)
    {
        var subject = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(subject, out userId))
        {
            problem = Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication failed",
                detail: "The access token is invalid.");
            return false;
        }

        problem = null;
        return true;
    }
}
