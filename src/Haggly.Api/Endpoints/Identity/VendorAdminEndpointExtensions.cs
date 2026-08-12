using Haggly.Api.Authorization;
using Haggly.Api.Responses;
using Haggly.Application.Common;
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
            .WithTags("Vendor administration")
            .RequireAuthorization(IdentityPolicies.AdminOnly);

        group.MapGet(string.Empty, GetVendorsAsync)
            .Produces<ApiResponse<PagedResult<VendorAdminDto>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

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
            ApiResponse<PagedResult<VendorAdminDto>>.Create(
                result,
                "Vendors retrieved successfully."));
    }
}
