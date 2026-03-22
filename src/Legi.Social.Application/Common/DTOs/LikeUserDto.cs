namespace Legi.Social.Application.Common.DTOs;

public class LikeUserDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = null!;
    public string? AvatarUrl { get; init; }
    
    /// <summary>
    /// Contextual flag — true if the current viewer follows this user.
    /// Always false for unauthenticated requests.
    /// </summary>
    public bool IsFollowedByViewer { get; init; }
}