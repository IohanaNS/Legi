namespace Legi.Contracts.Identity;

public sealed record UserUsernameChangedIntegrationEvent(
    Guid UserId,
    string NewUsername
) : IIntegrationEvent;
