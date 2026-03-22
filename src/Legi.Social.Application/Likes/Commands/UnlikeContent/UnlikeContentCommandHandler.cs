using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Likes.Commands.UnlikeContent;

public class UnlikeContentCommandHandler(
    ILikeRepository likeRepository)
    : IRequestHandler<UnlikeContentCommand>
{
    public async Task Handle(
        UnlikeContentCommand request,
        CancellationToken cancellationToken)
    {
        var like = await likeRepository.GetByUserAndTargetAsync(
            request.UserId, request.TargetType, request.TargetId, cancellationToken);
        if (like is null)
            throw new NotFoundException(nameof(Like),
                $"({request.UserId}, {request.TargetType}, {request.TargetId})");

        // Raises ContentUnlikedDomainEvent for future integration event to Library
        like.MarkForRemoval();

        await likeRepository.DeleteAsync(like, cancellationToken);
    }
}
