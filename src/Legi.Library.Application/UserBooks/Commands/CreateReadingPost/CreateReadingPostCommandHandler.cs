using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Repositories;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.CreateReadingPost;

public class CreateReadingPostCommandHandler(IUserBookRepository userBookRepository, IBookSnapshotRepository bookSnapshotRepository, IReadingPostRepository readingPostRepository)
    : IRequestHandler<CreateReadingPostCommand, CreateReadingPostResponse>
{
    public async Task<CreateReadingPostResponse> Handle(CreateReadingPostCommand request, CancellationToken cancellationToken)
    {
        var userBook = await userBookRepository.GetUserBook(request.UserBookId, request.BookId) ?? throw new NotFoundException("UserBook not found");
        
        if(userBook.UserId != request.UserId)
            throw new UnauthorizedAccessException("You do not have permission to create a reading post for this UserBook");
        
        Progress? progress = null;
        if(request.ProgressValue.HasValue && request.ProgressType.HasValue)
        {
            progress = Progress.Create(request.ProgressValue.Value, request.ProgressType.Value);
            
            if(progress.Type == ProgressType.Page)
            {
                var snapshot = await bookSnapshotRepository.GetByBookIdAsync(request.BookId, cancellationToken);
                if(snapshot == null)
                    throw new NotFoundException($"BookSnapshot for BookId {request.BookId} not found");

                if (snapshot.PageCount != null && progress.Value >= snapshot.PageCount.Value)
                {
                    progress = Progress.Completed();
                }
            }
            else if(progress.Type == ProgressType.Percentage)
            {
                if (progress.Value >= 100)
                {
                    progress = Progress.Completed();
                }
            }
        }
        var post = ReadingPost.Create(
            request.UserBookId,
            userBook.UserId,
            userBook.BookId,
            request.Content,
            progress,
            request.ReadingDate);
 
        if (progress is not null)
            userBook.UpdateProgress(progress);
        
        // 5. Persist both
        await readingPostRepository.AddAsync(post, cancellationToken);
        await userBookRepository.UpdateAsync(userBook, cancellationToken);
 
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
}