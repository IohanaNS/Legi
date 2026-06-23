namespace Legi.Identity.Application.Users.Queries.GetCurrentUser;

public record GetCurrentUserResponse(
    Guid UserId,
    string Email,
    string Username,
    DateTime CreatedAt,
    UserStatsDto Stats,
    bool MfaEnabled = false,
    // Active second-factor method as a string ("None" | "Totp" | "Email") so the client
    // can render the right settings state without depending on enum numbering.
    string MfaMethod = "None"
);

public record UserStatsDto(
    int TotalBooks,
    int TotalReviews,
    int TotalFollowers,
    int TotalFollowing
);
