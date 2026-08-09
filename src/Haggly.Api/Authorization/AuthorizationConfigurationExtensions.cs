using Haggly.Domain.Modules.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Haggly.Api.Authorization;

public static class AuthorizationConfigurationExtensions
{
    public static IServiceCollection AddHagglyAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(
                IdentityPolicies.BuyerOnly,
                policy => policy.RequireRole(RoleCode.BUYER.ToString()))
            .AddPolicy(
                IdentityPolicies.VendorOnly,
                policy => policy.RequireRole(RoleCode.VENDOR.ToString()))
            .AddPolicy(
                IdentityPolicies.AdminOnly,
                policy => policy.RequireRole(
                    RoleCode.MARKET_ADMIN.ToString(),
                    RoleCode.PLATFORM_ADMIN.ToString()));

        return services;
    }
}
