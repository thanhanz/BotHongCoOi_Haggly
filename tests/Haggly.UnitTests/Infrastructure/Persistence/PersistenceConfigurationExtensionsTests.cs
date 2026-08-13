using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.MediatR;
using Haggly.Application.Modules.Markets.Commands.Markets;
using Haggly.Application.Modules.Markets.Dtos.Markets;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence;

public sealed class PersistenceConfigurationExtensionsTests
{
    [Fact]
    public void AddPersistence_registers_haggly_db_context_with_postgresql()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:HagglyDatabase"] =
                "Host=localhost;Port=5433;Database=haggly;Username=postgres;Password=postgres"
        };

        services.AddPersistence(configuration);

        using var provider = services.BuildServiceProvider();
        using var context = provider.GetRequiredService<HagglyDbContext>();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }

    [Fact]
    public void AddPersistence_throws_when_connection_string_is_missing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddPersistence(configuration);
        });

        Assert.Contains("ConnectionStrings:HagglyDatabase", exception.Message);
    }

    [Fact]
    public void AddInfrastructureRepositories_returns_the_original_service_collection()
    {
        var services = new ServiceCollection();

        var result = services.AddInfrastructureRepositories();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddHagglyMediatR_registers_market_command_handlers()
    {
        var services = new ServiceCollection();

        services.AddHagglyMediatR();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IRequestHandler<CreateMarketCommand, MarketDto>));
    }
}
