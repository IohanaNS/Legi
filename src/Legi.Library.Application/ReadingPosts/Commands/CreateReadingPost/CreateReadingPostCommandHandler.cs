using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Repositories;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.ReadingPosts.Commands.CreateReadingPost;

public class CreateReadingPostCommandHandler
    : IRequestHandler<CreateReadingPostCommand, CreateReadingPostResponse>
{
    private readonly IReadingPostRepository _readingPostRepository;
    private readonly IUserBookRepository _userBookRepository;
    private readonly IBookSnapshotRepository _bookSnapshotRepository;

    public CreateReadingPostCommandHandler(
        IReadingPostRepository readingPostRepository,
        IUserBookRepository userBookRepository,
        IBookSnapshotRepository bookSnapshotRepository)
    {
        _readingPostRepository = readingPostRepository;
        _userBookRepository = userBookRepository;
        _bookSnapshotRepository = bookSnapshotRepository;
    }

    public async Task<CreateReadingPostResponse> Handle(
        CreateReadingPostCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load UserBook and verify ownership
        var userBook = await _userBookRepository.GetByIdAsync(
            request.UserBookId, cancellationToken)
            ?? throw new NotFoundException("UserBook", request.UserBookId);

        if (userBook.UserId != request.UserId)
            throw new ForbiddenException();

        // 2. Build Progress VO if provided
        Progress? progress = null;
        if (request.ProgressValue.HasValue && request.ProgressType.HasValue)
        {
            progress = Progress.Create(request.ProgressValue.Value, request.ProgressType.Value);

            // Page completion detection
            if (progress.Type == ProgressType.Page)
            {
                var snapshot = await _bookSnapshotRepository.GetByBookIdAsync(
                    userBook.BookId, cancellationToken);

                if (snapshot?.PageCount is not null
                    && progress.Value > snapshot.PageCount.Value)
                {
                    throw new SharedKernel.DomainException(
                        $"Page progress ({progress.Value}) cannot exceed book page count ({snapshot.PageCount.Value}).");
                }

                // If page equals total, convert to completed
                if (snapshot?.PageCount is not null
                    && progress.Value == snapshot.PageCount.Value)
                {
                    progress = Progress.Completed();
                }
            }
        }

        // 3. Create ReadingPost aggregate
        var post = ReadingProgress.Create(
            request.UserBookId,
            userBook.UserId,
            userBook.BookId,
            request.Content,
            progress,
            request.ReadingDate);

        // 4. Update UserBook's current progress (same transaction)
        if (progress is not null)
            userBook.UpdateProgress(progress);

        // 5. Persist both
        await _readingPostRepository.AddAsync(post, cancellationToken);
        await _userBookRepository.UpdateAsync(userBook, cancellationToken);

        // 6. Return response
        return new CreateReadingPostResponse(
            post.Id,
            post.UserBookId,
            post.Content,
            post.CurrentProgress?.Value,
            post.CurrentProgress?.Type.ToString(),
            post.ReadingDate,
            post.CreatedAt);
    }
}