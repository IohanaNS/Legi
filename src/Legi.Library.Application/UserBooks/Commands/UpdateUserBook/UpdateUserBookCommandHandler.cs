using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Repositories;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.UpdateUserBook;

public class UpdateUserBookCommandHandler : IRequestHandler<UpdateUserBookCommand, UpdateUserBookResponse>
{
    private readonly IUserBookRepository _userBookRepository;
    private readonly IBookSnapshotRepository _bookSnapshotRepository;
    
    public UpdateUserBookCommandHandler(IUserBookRepository userBookRepository, IBookSnapshotRepository bookSnapshotRepository)
    {
        _userBookRepository = userBookRepository;
        _bookSnapshotRepository = bookSnapshotRepository;
    }
    
    public async Task<UpdateUserBookResponse> Handle(UpdateUserBookCommand request, CancellationToken cancellationToken)
    {
        var userBook = await _userBookRepository.GetByIdAsync(request.UserBookId, cancellationToken) ?? throw new NotFoundException($"UserBook with ID {request.UserBookId} not found");
        
        if(userBook.UserId != request.UserId)
            throw new UnauthorizedAccessException("You do not have permission to update this UserBook");
        
        if(request.Status.HasValue)
            userBook.ChangeReadingStatus(request.Status.Value);
        
        if(request.Wishlist.HasValue)
            userBook.SetWishList(request.Wishlist.Value);

        if (request.ProgressValue.HasValue && request.ProgressType.HasValue)
        {
            var progress = Progress.Create(
                request.ProgressValue.Value,
                request.ProgressType.Value);

            if (progress.Type == ProgressType.Page)
            {
                var snapshot = await _bookSnapshotRepository.GetByBookIdAsync(userBook.BookId, cancellationToken);
                if(snapshot == null)
                    throw new NotFoundException($"BookSnapshot for BookId {userBook.BookId} not found");

                if (snapshot.PageCount != null && progress.Value >= snapshot.PageCount.Value)
                {
                    userBook.UpdateProgress(Progress.Completed());
                }
                else
                {
                    userBook.UpdateProgress(progress);
                }
            }
            else
            {
                userBook.UpdateProgress(progress);
            }
        }
        
        await _userBookRepository.UpdateAsync(userBook, cancellationToken);
        
        return new UpdateUserBookResponse(
            userBook.Id,
            userBook.BookId,
            userBook.Status.ToString(),
            userBook.CurrentProgress?.Value,
            userBook.CurrentProgress?.Type.ToString(),
            userBook.WishList,
            userBook.CurrentRating?.Stars,
            userBook.UpdatedAt);
    }
}