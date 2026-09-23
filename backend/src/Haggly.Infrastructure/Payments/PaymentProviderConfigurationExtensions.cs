using Haggly.Application.Abstractions.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Haggly.Infrastructure.Payments;

public static class PaymentProviderConfigurationExtensions
{
    public static IServiceCollection AddPaymentProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SimulatedPaymentOptions>()
            .Bind(configuration.GetSection(SimulatedPaymentOptions.SectionName))
            .Validate(options => options.IsValid(),
                "The simulated payment outcome and failure reason must be valid.")
            .ValidateOnStart();

        services.AddScoped<IPaymentProvider, SimulatedPaymentProvider>();
        return services;
    }
}
