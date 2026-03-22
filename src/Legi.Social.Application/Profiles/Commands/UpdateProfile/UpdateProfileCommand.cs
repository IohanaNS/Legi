using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Profiles.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string? Bio,
    string? AvatarUrl,
    string? BannerUrl) : IRequest<UpdateProfileResponse>;
