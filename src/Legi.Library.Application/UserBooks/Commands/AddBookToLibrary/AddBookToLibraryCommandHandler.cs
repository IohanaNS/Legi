using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.AddBookToLibrary;

public class AddBookToLibraryCommandHandler : IRequestHandler<AddBookToLibraryCommand, AddBookToLibraryResponse>
{
    private readonly IUserBookRepository _userBookRepository;
    private readonly IBookSnapshotRepository _bookSnapshotRepository;


    public AddBookToLibraryCommandHandler(IUserBookRepository userBookRepository, IBookSnapshotRepository bookSnapshotRepository)
    {
        _userBookRepository = userBookRepository;
        _bookSnapshotRepository = bookSnapshotRepository;
    }

    public async Task<AddBookToLibraryResponse> Handle(AddBookToLibraryCommand request, CancellationToken cancellationToken)
    {
        var bookSnapshot = await _bookSnapshotRepository.GetByBookIdAsync(request.BookId, cancellationToken);
        if (bookSnapshot == null)
        {
            if (string.IsNullOrWhiteSpace(request.BookTitle))
                throw new NotFoundException($"Book with ID {request.BookId} not found. Provide book details (bookTitle, bookAuthorDisplay) to create a snapshot.");

            bookSnapshot = BookSnapshot.Create(
                request.BookId,
                request.BookTitle,
                request.BookAuthorDisplay ?? "Unknown",
                request.BookCoverUrl,
                request.BookPageCount);
            await _bookSnapshotRepository.AddOrUpdateAsync(bookSnapshot, cancellationToken);
        }

        var existing = await _userBookRepository.GetByUserAndBookAsync(request.UserId, request.BookId, cancellationToken);
        if(existing != null)
            throw new ConflictException($"Book with ID {request.BookId} is already in the user's library.");
        
        var userBook = UserBook.Create(request.UserId, request.BookId, request.Wishlist);
        
        await _userBookRepository.AddAsync(userBook, cancellationToken);
        
        return new AddBookToLibraryResponse(userBook.Id, userBook.BookId, userBook.Status.ToString(), userBook.WishList, userBook.CreatedAt);
    }
}