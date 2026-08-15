using Haggly.Api.Authorization;
using Haggly.Api.Middleware;
using Haggly.Application.Common.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Haggly.Api;

public static class ApiConfigurationExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddHagglyAuthorization();
        services.AddSingleton<IBusinessClock>(_ => new BusinessClock(
            TimeProvider.System,
            FindInventoryTimeZone()));
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Haggly API",
                Version = "v1"
            });
            var bearerScheme = new OpenApiSecurityScheme
            {
              Name = "Authorization",
              Description = "Enter JWT token only.",
              In = ParameterLocation.Header,
              Type = SecuritySchemeType.Http,
              Scheme = "bearer",
              BearerFormat = "JWT"
            };

            options.AddSecurityDefinition("Bearer", bearerScheme);
            
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
              [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });
        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            ConfigureBearerEvents);

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Haggly API v1");
        });
        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

        return app;
    }

    private static void ConfigureBearerEvents(JwtBearerOptions options)
    {
        options.Events ??= new JwtBearerEvents();
        
        options.Events.OnAuthenticationFailed = context =>
        {
          Console.WriteLine(context.Exception.Message);
          return Task.CompletedTask;
        };
      
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

    private static TimeZoneInfo FindInventoryTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}
