namespace Legi.Identity.Application.Users.Commands.UpdateProfile;

public record UpdateProfileResponse(
    Guid UserId,
    string Name,
    string? Bio,
    string? AvatarUrl
);