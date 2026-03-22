using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Follows.Commands.UnfollowUser;

public record UnfollowUserCommand(Guid FollowerId, Guid FollowingId) : IRequest;