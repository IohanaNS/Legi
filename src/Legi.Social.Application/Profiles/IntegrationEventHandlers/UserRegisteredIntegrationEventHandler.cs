using Legi.Contracts.Identity;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Social.Application.Profiles.IntegrationEventHandlers;

/// <summary>
/// Social's consumer for <see cref="UserRegisteredIntegrationEvent"/>. Creates
/// the local <see cref="UserProfile"/> read model so Social can render the user
/// in feeds, follow lists, comments, etc.
///
/// Only UserId and Username are used — Social's UserProfile has no email field.
/// The event carries Email for other consumers (e.g. a future notification
/// service); Social ignores it. That's correct: integration events broadcast
/// the complete fact, and each consumer projects what it needs.
///
/// Idempotent: if a profile already exists for the user, this is a no-op (the
/// registration-time username must not overwrite a later rename).
///
/// MUST NOT call SaveChangesAsync — see MESSAGING-ARCHITECTURE-decisions.md
/// decision 8.1. The IntegrationEventDispatcher commits the inbox row and
/// the profile together in a single transaction.
/// </summary>
public sealed class UserRegisteredIntegrationEventHandler
    : INotificationHandler<UserRegisteredIntegrationEvent>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<UserRegisteredIntegrationEventHandler> _logger;

    public UserRegisteredIntegrationEventHandler(
        IUserProfileRepository userProfileRepository,
        ILogger<UserRegisteredIntegrationEventHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _logger = logger;
    }

    public async Task Handle(
        UserRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var profile = UserProfile.Create(
            integrationEvent.UserId,
            integrationEvent.Username);

        await _userProfileRepository.StageCreateIfMissingAsync(profile, cancellationToken);

        _logger.LogInformation(
            "Created UserProfile for user {UserId} ({Username}) from UserRegisteredIntegrationEvent",
            integrationEvent.UserId,
            integrationEvent.Username);
    }
}
