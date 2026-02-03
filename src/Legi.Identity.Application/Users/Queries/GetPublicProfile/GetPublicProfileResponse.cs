namespace Legi.Identity.Application.Users.Queries.GetPublicProfile;

public record GetPublicProfileResponse(
    Guid UserId,
    string Name,
    string? Bio,
    string? AvatarUrl,
    DateTime CreatedAt,
    PublicUserStatsDto Stats,
    bool? IsFollowedByMe  // Null se não autenticado
);

public record PublicUserStatsDto(
    int TotalBooks,
    int TotalReviews,
    int TotalFollowers,
    int TotalFollowing
);