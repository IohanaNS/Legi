namespace Legi.Social.Application.Common.DTOs;

public record UpdateProfileResponse(
    Guid UserId,
    string Username,
    string? Bio,
    string? AvatarUrl,
    string? BannerUrl,
    DateTime UpdatedAt);