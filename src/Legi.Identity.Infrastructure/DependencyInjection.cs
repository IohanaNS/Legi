using Legi.Contracts.Identity;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Domain.Repositories;
using Legi.Identity.Infrastructure.Persistence;
using Legi.Identity.Infrastructure.Persistence.Repositories;
using Legi.Identity.Infrastructure.Security;
using Legi.Messaging.DependencyInjection;
using Legi.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Legi.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddDbContext<IdentityDbContext>((sp, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("IdentityDb"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
                    npgsqlOptions.EnableRetryOnFailure(3);
                }
            ).AddInterceptors(sp.GetRequiredService<DispatchDomainEventsInterceptor>()));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();

        // Security
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // Messaging infrastructure (own outbox + RabbitMQ).
        // Registers IEventBus, OutboxDispatcherWorker, RabbitMQ connection/publisher,
        // and the integration event dispatcher for Identity.
        services.AddLegiMessaging<IdentityDbContext>("identity", configuration);

        // Register the smoke-test self-consumer.
        services.AddIntegrationEventConsumer<UserRegisteredIntegrationEvent, IdentityDbContext>();

        return services;
    }
}