using Haggly.Application.Modules.Identity.Login.Exceptions;
using Haggly.Application.Modules.Identity.Registration.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Haggly.Api.Middleware;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = Map(exception);

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "An unhandled exception occurred while processing the request.");
        }

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }

    private static (int Status, string Title, string Detail) Map(Exception exception)
        => exception switch
        {
            RegistrationValidationException or LoginValidationException
                => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            AuthenticationException
                => (StatusCodes.Status401Unauthorized, "Authentication failed", exception.Message),
            RegistrationConflictException
                => (StatusCodes.Status409Conflict, "Registration conflict", exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An unexpected error occurred while processing the request.")
        };
}
