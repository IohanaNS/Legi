using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Comments.Commands.DeleteComment;

public class DeleteCommentCommandHandler(
    ICommentRepository commentRepository,
    IContentSnapshotRepository contentSnapshotRepository)
    : IRequestHandler<DeleteCommentCommand>
{
    public async Task Handle(
        DeleteCommentCommand request,
        CancellationToken cancellationToken)
    {
        var comment = await commentRepository.GetByIdAsync(request.CommentId);
        if (comment is null)
            throw new NotFoundException(nameof(Comment), request.CommentId);

        // Authorization: comment author OR content owner can delete
        // The aggregate is ignorant about authorization — the handler resolves it.
        var isCommentAuthor = request.ActorId == comment.UserId;

        if (!isCommentAuthor)
        {
            // Check if actor is the content owner
            var snapshot = await contentSnapshotRepository.GetByTargetAsync(
                comment.TargetType, comment.TargetId);

            var isContentOwner = snapshot is not null && request.ActorId == snapshot.OwnerId;

            if (!isContentOwner)
                throw new ForbiddenException(
                    "Only the comment author or the content owner can delete this comment.");
        }

        // Raises CommentDeletedDomainEvent for future integration event to Library
        comment.MarkForDeletion();

        await commentRepository.DeleteAsync(comment);
    }
}