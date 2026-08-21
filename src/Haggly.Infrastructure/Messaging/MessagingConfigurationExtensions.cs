using Haggly.Application.Common.Messaging;
using Haggly.Infrastructure.Messaging.Outbox;
using Haggly.Infrastructure.Messaging.Serialization;
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
        
        services.AddScoped<IOutboxProcessor, DapperOutboxProcessor>();
        services.AddSingleton(provider => new DomainEventTypeRegistry(
            provider.GetServices<DomainEventTypeRegistration>()));
        return services;
    }

    public static IServiceCollection AddDomainEvent<TEvent>(
        this IServiceCollection services,
        string eventType)
        where TEvent : class, Haggly.Domain.Common.Events.V1.IDomainEvent
    {
        services.AddSingleton(DomainEventTypeRegistration.For<TEvent>(eventType));
        return services;
    }
}
