using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Haggly.Api.Endpoints.Identity.Requests;
using Haggly.Api.Endpoints.Identity.Responses;
using Haggly.Api.Responses;
using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Login.Commands;
using Haggly.Application.Modules.Identity.Registration.Commands;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Haggly.Api.Endpoints.Identity;

public static class IdentityEndpointExtensions
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(IdentityRoutes.Prefix)
                             .WithTags("Authentication");

        group.MapPost(IdentityRoutes.RegisterBuyer, RegisterBuyerAsync)
            .Produces<ApiResponse<RegistrationResponse>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        
        group.MapPost(IdentityRoutes.RegisterVendor, RegisterVendorAsync)
            .Produces<ApiResponse<RegistrationResponse>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        
        group.MapPost(IdentityRoutes.Login, LoginAsync)
            .Produces<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        
        group.MapGet(IdentityRoutes.CurrentUser, GetCurrentUser)
            .Produces<ApiResponse<CurrentUserResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterBuyerAsync(
        RegisterBuyerRequest request,
        [FromServices] IRegisterBuyerUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(
            new RegisterBuyerCommand(
                request.Email,
                request.PhoneNumber,
                request.Password,
                request.FullName),
            cancellationToken);

        return Results.Created(
            IdentityRoutes.CurrentUserLocation,
            ApiResponse<RegistrationResponse>.Create(
                RegistrationResponse.From(result),
                "Buyer registered successfully."));
    }

    private static async Task<IResult> RegisterVendorAsync(
        RegisterVendorRequest request,
        [FromServices] IRegisterVendorUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(
            new RegisterVendorCommand(
                request.Email,
                request.PhoneNumber,
                request.Password,
                request.FullName,
                request.BusinessName,
                request.BusinessRegistrationNo,
                request.TaxCode),
            cancellationToken);

        return Results.Created(
            IdentityRoutes.CurrentUserLocation,
            ApiResponse<RegistrationResponse>.Create(
                RegistrationResponse.From(result),
                "Vendor registered successfully."));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        [FromServices] ILoginUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.HandleAsync(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        return Results.Ok(
            ApiResponse<LoginResponse>.Create(
                LoginResponse.From(result),
                "Login successful."));
    }

    private static IResult GetCurrentUser(HttpContext httpContext)
    {
        var principal = httpContext.User;
        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(subject, out var userId))
            return Problem(StatusCodes.Status401Unauthorized, "Authentication failed", "The access token is invalid.");

        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? principal.FindFirstValue(ClaimTypes.Email);
        var roles = principal.FindAll("roles")
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return Results.Ok(
            ApiResponse<CurrentUserResponse>.Create(
                new CurrentUserResponse(userId, email, roles),
                "Current user retrieved successfully."));
    }

    private static IResult Problem(int statusCode, string title, string detail)
        => Results.Problem(statusCode: statusCode, title: title, detail: detail);
}
