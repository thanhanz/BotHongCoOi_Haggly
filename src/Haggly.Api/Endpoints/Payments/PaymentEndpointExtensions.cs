using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Haggly.Api.Authorization;
using Haggly.Api.Endpoints.Payments.Requests;
using Haggly.Api.Responses;
using Haggly.Application.Modules.Payments.Commands;
using Haggly.Application.Modules.Payments.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Haggly.Api.Endpoints.Payments;

public static class PaymentEndpointExtensions
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PaymentRoutes.Prefix)
            .WithTags("Payments")
            .RequireAuthorization(IdentityPolicies.BuyerOnly);

        group.MapPost(PaymentRoutes.Root, StartAsync)
            .Produces<ApiResponse<PaymentDto>>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> StartAsync(
        StartPaymentRequest request,
        HttpContext context,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new StartPaymentCommand(request.OrderId, CurrentUserId(context)),
            cancellationToken);

        return Results.Accepted(
            $"{PaymentRoutes.Prefix}/{result.Id}",
            ApiResponse<PaymentDto>.Create(result, "Payment accepted for asynchronous processing."));
    }

    private static Guid CurrentUserId(HttpContext context)
        => Guid.TryParse(
            context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var id)
            ? id
            : Guid.Empty;
}
