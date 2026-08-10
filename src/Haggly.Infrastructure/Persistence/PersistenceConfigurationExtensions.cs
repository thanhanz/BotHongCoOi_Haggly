using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Haggly.Application.Modules.Identity.Registration;
using Haggly.Application.Modules.Identity.Login;
using Haggly.Application.Abstractions.Identity;
using Haggly.Infrastructure.Authentication;
using Haggly.Infrastructure.Persistence.Repositories.Identity;

namespace Haggly.Infrastructure.Persistence;

public static class PersistenceConfigurationExtensions
{
    private const string ConnectionStringName = "HagglyDatabase";

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConfigurationPath.Combine("ConnectionStrings", ConnectionStringName)}' is required.");
        }

        services.AddDbContext<HagglyDbContext>(options => options.UseNpgsql(connectionString));
        services.AddInfrastructureRepositories();

        return services;
    }

    public static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services)
    {
        services.AddScoped<IIdentityRegistrationRepository, EfIdentityRegistrationRepository>();
        services.AddScoped<IIdentityLoginRepository, EfIdentityLoginRepository>();
        services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
        services.AddScoped<RegisterBuyerHandler>();
        services.AddScoped<RegisterVendorHandler>();
        
        // Register strategy handlers for use cases
        services.AddScoped<IRegisterBuyerUseCase>(provider =>
            provider.GetRequiredService<RegisterBuyerHandler>());
        services.AddScoped<IRegisterVendorUseCase>(provider =>
            provider.GetRequiredService<RegisterVendorHandler>());
        services.AddScoped<LoginHandler>();
        services.AddScoped<ILoginUseCase>(provider =>
            provider.GetRequiredService<LoginHandler>());

        return services;
    }
}
