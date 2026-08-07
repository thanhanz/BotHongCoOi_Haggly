using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // Register your repositories here, for example:
        // services.AddScoped<IYourRepository, YourRepositoryImplementation>();

        return services;
    }
}
