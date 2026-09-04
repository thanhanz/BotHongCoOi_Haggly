using Haggly.Api.Authorization;
using Haggly.Api.Responses;
using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Finance.Reports;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Haggly.Api.Endpoints.Finance;

public static class RevenueReportEndpointExtensions
{
    public static IEndpointRouteBuilder MapRevenueReportEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var vendorGroup = endpoints.MapGroup(RevenueReportRoutes.VendorPrefix)
            .WithTags("Vendor revenue reports")
            .RequireAuthorization(IdentityPolicies.VendorOnly);

        vendorGroup.MapGet(RevenueReportRoutes.Revenue, GetVendorRevenueAsync)
            .Produces<ApiResponse<VendorRevenueReportResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var adminGroup = endpoints.MapGroup(RevenueReportRoutes.AdminPrefix)
            .WithTags("Admin revenue reports")
            .RequireAuthorization(IdentityPolicies.AdminOnly);

        adminGroup.MapGet(RevenueReportRoutes.Revenue, GetAdminRevenueAsync)
            .Produces<ApiResponse<AdminRevenueReportResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> GetVendorRevenueAsync(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] SaleChannel? saleChannel,
        [FromQuery] Guid? stallId,
        [FromServices] IUserContext userContext,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetVendorRevenueReportQuery(
                userContext.UserId,
                from,
                to,
                saleChannel,
                stallId),
            cancellationToken);

        return Results.Ok(ApiResponse<VendorRevenueReportResponse>.Create(
            result,
            "Vendor revenue report retrieved successfully."));
    }

    private static async Task<IResult> GetAdminRevenueAsync(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] SaleChannel? saleChannel,
        [FromQuery] Guid? marketId,
        [FromQuery] Guid? vendorId,
        [FromQuery] Guid? stallId,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetAdminRevenueReportQuery(
                from,
                to,
                saleChannel,
                marketId,
                vendorId,
                stallId),
            cancellationToken);

        return Results.Ok(ApiResponse<AdminRevenueReportResponse>.Create(
            result,
            "Admin revenue report retrieved successfully."));
    }
}
