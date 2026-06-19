namespace Legi.Social.Domain.Enums;

/// <summary>
/// What happened to the recipient's content. Orthogonal to
/// <see cref="InteractableType"/> (which answers *what content*: Post/Review/List).
/// Both axes are needed to render a notification ("liked your post").
/// </summary>
public enum NotificationType
{
    Like,
    Comment
}
