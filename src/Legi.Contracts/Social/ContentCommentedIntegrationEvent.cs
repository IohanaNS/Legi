namespace Legi.Contracts.Social;

/// <summary>
/// Published when a user comments on interactable content. Library consumes this
/// to increment <c>CommentsCount</c> on the target aggregate (Post → ReadingProgress,
/// List → UserList), located via <see cref="TargetType"/> + <see cref="TargetId"/>.
///
/// Note the name mapping: the in-process event is <c>CommentCreatedDomainEvent</c>,
/// but the cross-context event is named for the action on the target content.
/// <see cref="CommentId"/> / <see cref="UserId"/> are carried for traceability
/// only; Library does not read them.
/// </summary>
public sealed record ContentCommentedIntegrationEvent(
    string TargetType,
    Guid TargetId,
    Guid CommentId,
    Guid UserId
) : IIntegrationEvent;
