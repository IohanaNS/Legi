using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Likes.Commands.LikeContent;

public class LikeContentCommandHandler(
    ILikeRepository likeRepository,
    IContentSnapshotRepository contentSnapshotRepository)
    : IRequestHandler<LikeContentCommand, LikeResponse>
{
    public async Task<LikeResponse> Handle(
        LikeContentCommand request,
        CancellationToken cancellationToken)
    {
        // Verify content exists
        var snapshot = await contentSnapshotRepository.GetByTargetAsync(
            request.TargetType, request.TargetId);
        if (snapshot is null)
            throw new NotFoundException(nameof(ContentSnapshot),
                $"({request.TargetType}, {request.TargetId})");

        // Check uniqueness — user can like same content only once
        var existing = await likeRepository.GetByUserAndTargetAsync(
            request.UserId, request.TargetType, request.TargetId, cancellationToken);
        if (existing is not null)
            throw new ConflictException(
                $"User {request.UserId} has already liked {request.TargetType} {request.TargetId}.");

        var like = Like.Create(request.UserId, request.TargetType, request.TargetId);

        await likeRepository.AddAsync(like, cancellationToken);

        // ContentLikedDomainEvent is dispatched by DbContext on SaveChanges,
        // triggering the stub handler (future: integration event to Library).

        return new LikeResponse(like.Id, like.CreatedAt);
    }
}
