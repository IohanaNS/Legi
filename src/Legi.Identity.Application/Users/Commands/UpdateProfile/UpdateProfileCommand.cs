using MediatR;

namespace Legi.Identity.Application.Users.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string? Name,
    string? Bio,
    string? AvatarUrl
) : IRequest<UpdateProfileResponse>;