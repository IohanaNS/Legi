namespace Legi.Social.Application.Common.DTOs;

public class UserProfileDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = null!;
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public string? BannerUrl { get; init; }
    public int FollowersCount { get; init; }
    public int FollowingCount { get; init; }
    
    /// <summary>
    /// Contextual flag — true if the current viewer follows this user.
    /// Always false for unauthenticated requests or when viewing own profile.
    /// </summary>
    public bool IsFollowing { get; init; }
    
    public DateTime CreatedAt { get; init; }
}