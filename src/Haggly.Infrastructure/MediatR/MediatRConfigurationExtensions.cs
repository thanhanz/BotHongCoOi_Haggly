using Haggly.Application;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Haggly.Infrastructure.MediatR;

public static class MediatRConfigurationExtensions
{
    public static IServiceCollection AddHagglyMediatR(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));

        return services;
    }
}
