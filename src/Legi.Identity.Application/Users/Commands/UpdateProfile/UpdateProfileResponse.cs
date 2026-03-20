namespace Legi.Identity.Application.Users.Commands.UpdateProfile;

public record UpdateProfileResponse(
    Guid UserId,
    bool IsPublicProfile
);
