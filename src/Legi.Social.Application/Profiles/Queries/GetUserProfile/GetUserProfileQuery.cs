using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Profiles.Queries.GetUserProfile;

public record GetUserProfileQuery(
    Guid TargetUserId,
    Guid? ViewerUserId) : IRequest<UserProfileDto>;