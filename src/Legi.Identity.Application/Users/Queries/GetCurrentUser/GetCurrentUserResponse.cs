namespace Legi.Identity.Application.Users.Queries.GetCurrentUser;

public record GetCurrentUserResponse(
    Guid UserId,
    string Email,
    string Username,
    DateTime CreatedAt,
    UserStatsDto Stats,
    bool MfaEnabled = false
);

public record UserStatsDto(
    int TotalBooks,
    int TotalReviews,
    int TotalFollowers,
    int TotalFollowing
);
