using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.ReadingPosts.Commands.DeleteReadingPost;

public class DeleteReadingPostCommandHandler
    : IRequestHandler<DeleteReadingPostCommand, Unit>
{
    private readonly IReadingPostRepository _readingPostRepository;

    public DeleteReadingPostCommandHandler(IReadingPostRepository readingPostRepository)
    {
        _readingPostRepository = readingPostRepository;
    }

    public async Task<Unit> Handle(
        DeleteReadingPostCommand request,
        CancellationToken cancellationToken)
    {
        var post = await _readingPostRepository.GetByIdAsync(
                       request.PostId, cancellationToken)
                   ?? throw new NotFoundException("ReadingPost", request.PostId);

        if (post.UserId != request.UserId)
            throw new ForbiddenException();

        post.Delete(); // Emits ReadingPostDeletedDomainEvent

        await _readingPostRepository.DeleteAsync(post, cancellationToken);

        return Unit.Value;
    }
}