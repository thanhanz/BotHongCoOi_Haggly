using Haggly.Application.Common.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Haggly.Infrastructure.Messaging;

public static class MessagingConfigurationExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(options => options.IsValid(),
                "RabbitMq configuration requires host, port, virtual host, username, and password.")
            .ValidateOnStart();

        services.AddMassTransit(configurator =>
        {
            configurator.UsingRabbitMq((context, rabbitMq) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                rabbitMq.Host(
                    options.Host,
                    options.Port,
                    options.VirtualHost,
                    host =>
                    {
                        host.Username(options.Username);
                        host.Password(options.Password);
                    });
            });
        });

        services.AddScoped<IDomainEventPublisher, MassTransitDomainEventPublisher>();
        return services;
    }
}
