using Legi.SharedKernel;

namespace Legi.Social.Domain.Entities;

/// <summary>
/// Aggregate root for the user's public social profile.
/// Does not inherit BaseEntity — UserId (from Identity) is the PK.
/// Bio, Avatar, and Banner are owned here, not in Identity.
/// Username is a snapshot from Identity, updated via integration events.
/// </summary>
public class UserProfile
{
    public const int MaxBioLength = 500;
 
    public Guid UserId { get; private set; }
    public string Username { get; private set; } = null!;
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public int FollowersCount { get; private set; }
    public int FollowingCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    internal static UserProfile Create(Guid userId, string userName)
    {
        return new UserProfile
        {
            UserId = userId,
            Username = userName,
            FollowersCount = 0,
            FollowingCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateBio(string? bio)
    {
        if (bio is not null && bio.Length > MaxBioLength)
            throw new DomainException($"Bio cannot exceed {MaxBioLength} characters");
 
        Bio = bio;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateAvatar(string? avatarUrl)
    {
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateBanner(string? bannerUrl)
    {
        BannerUrl = bannerUrl;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Called when Identity notifies that the user changed their username.
    /// </summary>
    public void UpdateUsername(string username)
    {
        Username = username;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void IncrementFollowers()
    {
        FollowersCount++;
        UpdatedAt = DateTime.UtcNow;
    }
 
    public void DecrementFollowers()
    {
        if (FollowersCount <= 0)
            throw new DomainException("Followers count cannot be negative");
 
        FollowersCount--;
        UpdatedAt = DateTime.UtcNow;
    }
 
    public void IncrementFollowing()
    {
        FollowingCount++;
        UpdatedAt = DateTime.UtcNow;
    }
 
    public void DecrementFollowing()
    {
        if (FollowingCount <= 0)
            throw new DomainException("Following count cannot be negative");
 
        FollowingCount--;
        UpdatedAt = DateTime.UtcNow;
    }
}