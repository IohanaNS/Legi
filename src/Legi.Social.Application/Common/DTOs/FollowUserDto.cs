namespace Legi.Social.Application.Common.DTOs;

public class FollowUserDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    
    /// <summary>
    /// Contextual flag — true if the current viewer follows this user.
    /// Enables the "Follow" / "Following" button state in the UI.
    /// Always false for unauthenticated requests.
    /// </summary>
    public bool IsFollowedByViewer { get; init; }
}