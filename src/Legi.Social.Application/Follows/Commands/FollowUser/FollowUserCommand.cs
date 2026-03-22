using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Follows.Commands.FollowUser;

public record FollowUserCommand(Guid FollowerId, Guid FollowingId) : IRequest<FollowResponse>;