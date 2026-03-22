using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Comments.Commands.CreateComment;

public class CreateCommentCommandHandler(
    ICommentRepository commentRepository,
    IContentSnapshotRepository contentSnapshotRepository)
    : IRequestHandler<CreateCommentCommand, CreateCommentResponse>
{
    public async Task<CreateCommentResponse> Handle(
        CreateCommentCommand request,
        CancellationToken cancellationToken)
    {
        // Verify content exists
        var snapshot = await contentSnapshotRepository.GetByTargetAsync(
            request.TargetType, request.TargetId);
        if (snapshot is null)
            throw new NotFoundException(nameof(ContentSnapshot),
                $"({request.TargetType}, {request.TargetId})");

        // Create — aggregate validates content length internally
        var comment = Comment.Create(
            request.UserId, request.TargetType, request.TargetId, request.Content);

        await commentRepository.AddAsync(comment);

        // CommentCreatedDomainEvent is dispatched by DbContext on SaveChanges,
        // triggering the stub handler (future: integration event to Library).

        return new CreateCommentResponse(comment.Id, comment.CreatedAt);
    }
}