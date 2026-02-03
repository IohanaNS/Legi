namespace Legi.Identity.Application.Users.Queries.GetCurrentUser;

public record GetCurrentUserResponse(
    Guid UserId,
    string Email,
    string Name,
    string? Bio,
    string? AvatarUrl,
    DateTime CreatedAt,
    UserStatsDto Stats
);

public record UserStatsDto(
    int TotalBooks,
    int TotalReviews,
    int TotalFollowers,
    int TotalFollowing
);