using Haggly.Application.Abstractions.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Haggly.Infrastructure.Authentication;

public static class TokenConfigurationExtensions
{
    public static IServiceCollection AddTokenServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => options.IsValid(),
                "Jwt configuration requires issuer, audience, a signing key of at least 32 characters, and an access-token lifetime between 1 and 1440 minutes.")
            .ValidateOnStart();

        services.AddSingleton<IIdentityTokenService, JwtTokenService>();
        return services;
    }
}
