namespace Legi.Contracts.Social;

/// <summary>
/// Published when a user likes interactable content. Library consumes this to
/// increment <c>LikesCount</c> on the target aggregate (Post → ReadingProgress,
/// List → UserList), located via <see cref="TargetType"/> + <see cref="TargetId"/>.
/// <see cref="UserId"/> is carried for traceability only; Library does not read it.
/// </summary>
public sealed record ContentLikedIntegrationEvent(
    string TargetType,
    Guid TargetId,
    Guid UserId
) : IIntegrationEvent;
