using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Follows.Commands.FollowUser;

public class FollowUserCommandHandler(
    IFollowRepository followRepository,
    IUserProfileRepository userProfileRepository)
    : IRequestHandler<FollowUserCommand, FollowResponse>
{
    public async Task<FollowResponse> Handle(
        FollowUserCommand request,
        CancellationToken cancellationToken)
    {
        // Verify target user exists
        var targetProfile = await userProfileRepository.GetByUserIdAsync(request.FollowingId);
        if (targetProfile is null)
            throw new NotFoundException(nameof(UserProfile), request.FollowingId);

        // Check not already following
        var existingFollow = await followRepository.GetByPairAsync(
            request.FollowerId, request.FollowingId);
        if (existingFollow is not null)
            throw new ConflictException("You are already following this user.");

        // Create — aggregate validates self-follow
        var follow = Follow.Create(request.FollowerId, request.FollowingId);

        await followRepository.AddAsync(follow);

        // FollowCreatedDomainEvent is dispatched by DbContext on SaveChanges,
        // triggering FollowCreatedDomainEventHandler to update UserProfile counters.

        return new FollowResponse(follow.Id, follow.CreatedAt);
    }
}