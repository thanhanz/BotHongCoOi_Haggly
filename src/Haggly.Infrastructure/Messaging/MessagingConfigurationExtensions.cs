using Haggly.Application.Common.Messaging;
using Haggly.Infrastructure.Messaging.Outbox;
using Haggly.Infrastructure.Messaging.Serialization;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Infrastructure.Messaging.Consumers;
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

        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .Validate(options => options.IsValid(),
                "Enabled Outbox configuration requires a positive interval and batch size.")
            .ValidateOnStart();

        services.AddMassTransit(configurator =>
        {
            configurator.AddConsumer<PaymentRequestedMassTransitConsumer>();

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

                rabbitMq.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IDomainEventPublisher, MassTransitDomainEventPublisher>();
        services.AddScoped<IOutboxWriter, DapperOutboxWriter>();
        services.AddScoped<IOutboxProcessor, DapperOutboxProcessor>();

        services.AddScoped<IDomainEventConsumer<PaymentRequested>, ProcessPaymentRequestedConsumer>();
        
        services.AddHostedService<OutboxBackgroundService>();
        services.AddSingleton(provider => new DomainEventTypeRegistry(
            provider.GetServices<DomainEventTypeRegistration>()));
        
        services.AddDomainEvent<PaymentRequested>("payments.payment-requested.v1");
        services.AddDomainEvent<PaymentSucceeded>("payments.payment-succeeded.v1");
        services.AddDomainEvent<PaymentFailed>("payments.payment-failed.v1");
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
