using Legi.Contracts;
using Legi.Messaging.Inbox;
using Legi.Messaging.Outbox;
using Legi.Messaging.RabbitMq;
using Legi.Messaging.Serialization;
using Legi.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Legi.Messaging.DependencyInjection;

/// <summary>
/// DI registration entry points for Legi.Messaging.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, section 7.3.
/// </summary>
public static class MessagingExtensions
{
    /// <summary>
    /// Registers the messaging infrastructure for a service: the outbox-backed
    /// <see cref="IEventBus"/>, the RabbitMQ publisher, the dispatcher worker,
    /// the integration event dispatcher, the connection factory, and the
    /// shared serializer.
    /// 
    /// Bind options from the "RabbitMq" and "Outbox" sections of configuration.
    /// 
    /// Both producer and consumer infrastructure are registered. A service that
    /// only consumes events will run an outbox dispatcher worker against an
    /// empty table; the cost is one indexed query per polling interval, which
    /// is negligible.
    /// </summary>
    /// <param name="services">The DI container.</param>
    /// <param name="serviceName">Lowercase short name for the consuming
    /// service (e.g. "identity"). Used as the prefix for RabbitMQ queue
    /// names.</param>
    /// <param name="configuration">Application configuration root.</param>
    public static IServiceCollection AddLegiMessaging<TContext>(
        this IServiceCollection services,
        string serviceName,
        IConfiguration configuration)
        where TContext : DbContext
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name is required.", nameof(serviceName));

        services.Configure<RabbitMqSettings>(
            configuration.GetSection(RabbitMqSettings.SectionName));

        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        services.Configure<MessagingHostingOptions>(opts =>
        {
            opts.ServiceName = serviceName.ToLowerInvariant();
        });

        // Connection is process-wide and expensive; singleton lifetime.
        services.AddSingleton<RabbitMqConnectionFactory>();

        // Stateless infrastructure components — singleton for efficiency.
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddSingleton<IntegrationEventSerializer>();
        services.AddSingleton<IntegrationEventDispatcher<TContext>>();

        // Producer-side: writes outbox rows in the current DbContext scope.
        // Scoped lifetime because the DbContext it depends on is scoped.
        services.AddScoped<IEventBus, OutboxEventBus<TContext>>();

        // Producer-side worker: polls the outbox and publishes to RabbitMQ.
        services.AddHostedService<OutboxDispatcherWorker<TContext>>();

        return services;
    }

    /// <summary>
    /// Registers a consumer host for a specific integration event type. Each
    /// call adds one <see cref="Microsoft.Extensions.Hosting.BackgroundService"/>
    /// that owns one channel and one queue. Call once per event type that the
    /// service consumes.
    /// </summary>
    public static IServiceCollection AddIntegrationEventConsumer<TEvent, TContext>(
        this IServiceCollection services)
        where TEvent : class, IIntegrationEvent
        where TContext : DbContext
    {
        services.AddHostedService<RabbitMqConsumerHost<TEvent, TContext>>();
        return services;
    }
}