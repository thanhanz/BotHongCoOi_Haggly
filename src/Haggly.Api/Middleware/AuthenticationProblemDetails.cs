using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Haggly.Api.Middleware;

internal static class AuthenticationProblemDetails
{
    public static async Task WriteAsync(
        HttpContext httpContext,
        int status,
        string title,
        string detail)
    {
        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        var service = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await service.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }
}
