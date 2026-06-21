using Legi.Contracts.Identity;
using Legi.Identity.Application.Auth.Commands.Login;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Repositories;
using Legi.Identity.Infrastructure.Email;
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
        services.AddScoped<ILoginAttemptRepository, LoginAttemptRepository>();

        // Security
        services.AddOptions<LoginLockoutSettings>()
            .Bind(configuration.GetSection(LoginLockoutSettings.SectionName))
            .Validate(
                LoginLockoutSettings.HasValidSettings,
                LoginLockoutSettings.ValidationMessage)
            .ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<LoginLockoutSettings>>().Value);

        services.AddOptions<TurnstileSettings>()
            .Bind(configuration.GetSection(TurnstileSettings.SectionName))
            .Validate(
                TurnstileSettings.HasValidSettings,
                TurnstileSettings.ValidationMessage)
            .ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<TurnstileSettings>>().Value);
        services.AddHttpClient<IHumanVerificationService, TurnstileVerificationService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<TurnstileSettings>();
            client.Timeout = TimeSpan.FromSeconds(settings.VerificationTimeoutSeconds);
        });

        // Breached-password check (Have I Been Pwned, k-anonymity). Fails open on outage.
        services.AddOptions<BreachedPasswordSettings>()
            .Bind(configuration.GetSection(BreachedPasswordSettings.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<BreachedPasswordSettings>>().Value);
        services.AddHttpClient<IBreachedPasswordChecker, HaveIBeenPwnedPasswordChecker>((sp, client) =>
        {
            var settings = sp.GetRequiredService<BreachedPasswordSettings>();
            client.BaseAddress = new Uri(settings.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            // HIBP requests a descriptive User-Agent.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BukiHub-PwnedPasswordCheck");
        });

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(
                // Identity signs tokens, so it needs both keys (private + public).
                JwtSettings.HasValidSigningSettings,
                JwtSettings.SigningValidationMessage)
            .ValidateOnStart();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ISecurityAuditLogger, SecurityAuditLogger>();

        // MFA (TOTP). The secret protector is resolved only when MFA is used, so the app
        // still boots without Mfa:EncryptionKey configured (it throws when first used).
        services.AddOptions<MfaSettings>()
            .Bind(configuration.GetSection(MfaSettings.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<MfaSettings>>().Value);
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<IMfaSecretProtector, AesMfaSecretProtector>();

        // Google sign-in. Bound without ValidateOnStart so the app still boots when
        // ClientId is unset — Google sign-in simply rejects all tokens until configured.
        services.AddOptions<GoogleAuthSettings>()
            .Bind(configuration.GetSection(GoogleAuthSettings.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<GoogleAuthSettings>>().Value);
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

        // Password reset (token factory + email delivery)
        services.AddOptions<PasswordResetSettings>()
            .Bind(configuration.GetSection(PasswordResetSettings.SectionName))
            .Validate(
                PasswordResetSettings.HasValidSettings,
                PasswordResetSettings.ValidationMessage)
            .ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<PasswordResetSettings>>().Value);

        services.AddOptions<EmailConfirmationSettings>()
            .Bind(configuration.GetSection(EmailConfirmationSettings.SectionName))
            .Validate(
                EmailConfirmationSettings.HasValidSettings,
                EmailConfirmationSettings.ValidationMessage)
            .ValidateOnStart();
        services.AddSingleton(sp => sp.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<EmailConfirmationSettings>>().Value);

        services.AddOptions<SmtpSettings>()
            .Bind(configuration.GetSection(SmtpSettings.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<SmtpSettings>>().Value);

        services.AddScoped<ISecureTokenFactory, SecureTokenFactory>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Messaging infrastructure (own outbox + RabbitMQ).
        // Registers IEventBus, OutboxDispatcherWorker, RabbitMQ connection/publisher,
        // and the integration event dispatcher for Identity.
        services.AddLegiMessaging<IdentityDbContext>("identity", configuration);

        // Register the smoke-test self-consumer.
        services.AddIntegrationEventConsumer<UserRegisteredIntegrationEvent, IdentityDbContext>();

        return services;
    }
}
