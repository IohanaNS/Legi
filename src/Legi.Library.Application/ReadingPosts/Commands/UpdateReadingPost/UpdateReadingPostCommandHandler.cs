using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Repositories;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.ReadingPosts.Commands.UpdateReadingPost;

public class UpdateReadingPostCommandHandler
    : IRequestHandler<UpdateReadingPostCommand, UpdateReadingPostResponse>
{
    private readonly IReadingPostRepository _readingPostRepository;
    private readonly IUserBookRepository _userBookRepository;
    private readonly IBookSnapshotRepository _bookSnapshotRepository;

    public UpdateReadingPostCommandHandler(
        IReadingPostRepository readingPostRepository,
        IUserBookRepository userBookRepository,
        IBookSnapshotRepository bookSnapshotRepository)
    {
        _readingPostRepository = readingPostRepository;
        _userBookRepository = userBookRepository;
        _bookSnapshotRepository = bookSnapshotRepository;
    }

    public async Task<UpdateReadingPostResponse> Handle(
        UpdateReadingPostCommand request,
        CancellationToken cancellationToken)
    {
        var post = await _readingPostRepository.GetByIdAsync(
                       request.PostId, cancellationToken)
                   ?? throw new NotFoundException("ReadingPost", request.PostId);

        if (post.UserId != request.UserId)
            throw new ForbiddenException();

        // Build Progress VO if provided
        Progress? progress = null;
        if (request.ProgressValue.HasValue && request.ProgressType.HasValue)
        {
            progress = Progress.Create(request.ProgressValue.Value, request.ProgressType.Value);

            if (progress.Type == ProgressType.Page)
            {
                var userBook = await _userBookRepository.GetByIdAsync(
                    post.UserBookId, cancellationToken);

                var snapshot = await _bookSnapshotRepository.GetByBookIdAsync(
                    post.BookId, cancellationToken);

                if (snapshot?.PageCount is not null
                    && progress.Value > snapshot.PageCount.Value)
                {
                    throw new SharedKernel.DomainException(
                        $"Page progress ({progress.Value}) cannot exceed book page count ({snapshot.PageCount.Value}).");
                }

                if (snapshot?.PageCount is not null
                    && progress.Value == snapshot.PageCount.Value)
                {
                    progress = Progress.Completed();
                }

                // Update UserBook's progress if this is the latest post
                if (userBook is not null)
                {
                    userBook.UpdateProgress(progress);
                    await _userBookRepository.UpdateAsync(userBook, cancellationToken);
                }
            }
        }

        post.Update(request.Content, progress);

        await _readingPostRepository.UpdateAsync(post, cancellationToken);

        return new UpdateReadingPostResponse(
            post.Id,
            post.Content,
            post.CurrentProgress?.Value,
            post.CurrentProgress?.Type.ToString(),
            post.UpdatedAt);
    }
}
