namespace Legi.Identity.Application.Users.Queries.GetPublicProfile;

public record GetPublicProfileResponse(
    Guid UserId,
    string Username,
    bool IsPublicProfile,
    DateTime CreatedAt,
    PublicUserStatsDto Stats,
    bool? IsFollowedByMe
);

public record PublicUserStatsDto(
    int TotalBooks,
    int TotalReviews,
    int TotalFollowers,
    int TotalFollowing
);
