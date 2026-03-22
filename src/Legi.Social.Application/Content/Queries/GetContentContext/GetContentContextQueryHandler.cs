using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Content.Queries.GetContentContext;

public class GetContentContextQueryHandler(
    IContentSnapshotRepository contentSnapshotRepository)
    : IRequestHandler<GetContentContextQuery, ContentContextDto>
{
    public async Task<ContentContextDto> Handle(
        GetContentContextQuery request,
        CancellationToken cancellationToken)
    {
        var snapshot = await contentSnapshotRepository.GetByTargetAsync(
            request.TargetType, request.TargetId, cancellationToken);

        if (snapshot is null)
            throw new NotFoundException(nameof(ContentSnapshot),
                $"({request.TargetType}, {request.TargetId})");

        return new ContentContextDto
        {
            TargetType = snapshot.TargetType.ToString(),
            TargetId = snapshot.TargetId,
            OwnerId = snapshot.OwnerId,
            OwnerUsername = snapshot.OwnerUsername,
            OwnerAvatarUrl = snapshot.OwnerAvatarUrl,
            BookTitle = snapshot.BookTitle,
            BookAuthor = snapshot.BookAuthor,
            BookCoverUrl = snapshot.BookCoverUrl,
            ContentPreview = snapshot.ContentPreview
        };
    }
}