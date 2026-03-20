using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    bool IsPublicProfile
) : IRequest<UpdateProfileResponse>;
