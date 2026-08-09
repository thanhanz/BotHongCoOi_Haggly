using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Haggly.Api.Endpoints.Identity.Requests;
using Haggly.Api.Endpoints.Identity.Responses;
using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Login.Commands;
using Haggly.Application.Modules.Identity.Login.Exceptions;
using Haggly.Application.Modules.Identity.Registration.Commands;
using Haggly.Application.Modules.Identity.Registration.Exceptions;
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
        var group = endpoints.MapGroup("/api/auth");

        group.MapPost("/register/buyer", RegisterBuyerAsync);
        group.MapPost("/register/vendor", RegisterVendorAsync);
        group.MapPost("/login", LoginAsync);
        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterBuyerAsync(
        RegisterBuyerRequest request,
        [FromServices] IRegisterBuyerUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCase.HandleAsync(
                new RegisterBuyerCommand(
                    request.Email,
                    request.PhoneNumber,
                    request.Password,
                    request.FullName),
                cancellationToken);

            return Results.Created(
                "/api/auth/me",
                RegistrationResponse.From(result));
        }
        catch (RegistrationValidationException exception)
        {
            return Problem(StatusCodes.Status400BadRequest, "Registration validation failed", exception.Message);
        }
        catch (RegistrationConflictException exception)
        {
            return Problem(StatusCodes.Status409Conflict, "Registration conflict", exception.Message);
        }
    }

    private static async Task<IResult> RegisterVendorAsync(
        RegisterVendorRequest request,
        [FromServices] IRegisterVendorUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
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
                "/api/auth/me",
                RegistrationResponse.From(result));
        }
        catch (RegistrationValidationException exception)
        {
            return Problem(StatusCodes.Status400BadRequest, "Registration validation failed", exception.Message);
        }
        catch (RegistrationConflictException exception)
        {
            return Problem(StatusCodes.Status409Conflict, "Registration conflict", exception.Message);
        }
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        [FromServices] ILoginUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await useCase.HandleAsync(
                new LoginCommand(request.Email, request.Password),
                cancellationToken);

            return Results.Ok(LoginResponse.From(result));
        }
        catch (LoginValidationException exception)
        {
            return Problem(StatusCodes.Status400BadRequest, "Login validation failed", exception.Message);
        }
        catch (AuthenticationException exception)
        {
            return Problem(StatusCodes.Status401Unauthorized, "Authentication failed", exception.Message);
        }
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
        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return Results.Ok(new CurrentUserResponse(userId, email, roles));
    }

    private static IResult Problem(int statusCode, string title, string detail)
        => Results.Problem(statusCode: statusCode, title: title, detail: detail);
}
