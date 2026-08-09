using Haggly.Api.Authorization;
using Haggly.Api.Middleware;
using Haggly.Api.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Haggly.Api;

public static class ApiConfigurationExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddHagglyAuthorization();
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "Haggly API";
                document.Info.Version = "v1";
                return Task.CompletedTask;
            });
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<AuthorizationOperationTransformer>();
        });
        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            ConfigureBearerEvents);

        return services;
    }

    private static void ConfigureBearerEvents(JwtBearerOptions options)
    {
        options.Events ??= new JwtBearerEvents();
        options.Events.OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;
            await AuthenticationProblemDetails.WriteAsync(
                context.HttpContext,
                StatusCodes.Status401Unauthorized,
                "Authentication required",
                "A valid bearer access token is required.");
        };
        options.Events.OnForbidden = context => AuthenticationProblemDetails.WriteAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Access forbidden",
            "The authenticated user does not have permission to access this resource.");
    }
}
