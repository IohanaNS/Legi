namespace Legi.Contracts.Social;

/// <summary>
/// Published when a user removes a like. Library consumes this to decrement
/// <c>LikesCount</c> on the target aggregate (Post → ReadingProgress,
/// List → UserList), located via <see cref="TargetType"/> + <see cref="TargetId"/>.
/// <see cref="UserId"/> is carried for traceability only; Library does not read it.
/// </summary>
public sealed record ContentUnlikedIntegrationEvent(
    string TargetType,
    Guid TargetId,
    Guid UserId
) : IIntegrationEvent;
