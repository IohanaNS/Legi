using Legi.Contracts.Identity;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Application.Auth.EventHandlers;

/// <summary>
/// Identity's self-consumer for <see cref="UserRegisteredIntegrationEvent"/>.
/// Logs reception only. Exists to:
/// <list type="bullet">
///   <item>Validate the messaging pipeline end-to-end during the Phase 1 smoke test</item>
///   <item>Serve as a permanent canary in production: if this stops logging,
///         the messaging pipeline is broken</item>
/// </list>
/// 
/// Identity does not act on its own UserRegistered events — the actual
/// consumers (Library, Social) live in their respective services and will
/// be wired up in Phase 5.
/// 
/// MUST NOT call SaveChangesAsync — see MESSAGING-ARCHITECTURE-decisions.md
/// decision 8.1.
/// </summary>
public sealed class UserRegisteredIntegrationEventHandler
    : INotificationHandler<UserRegisteredIntegrationEvent>
{
    private readonly ILogger<UserRegisteredIntegrationEventHandler> _logger;

    public UserRegisteredIntegrationEventHandler(
        ILogger<UserRegisteredIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(
        UserRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received UserRegisteredIntegrationEvent: user {UserId} ({Username}) registered at {RegisteredAt}",
            integrationEvent.UserId,
            integrationEvent.Username,
            integrationEvent.RegisteredAt);

        return Task.CompletedTask;
    }
}