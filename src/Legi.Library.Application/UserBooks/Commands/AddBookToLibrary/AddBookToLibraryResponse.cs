namespace Legi.Library.Application.UserBooks.Commands.AddBookToLibrary;

public record AddBookToLibraryResponse(
    Guid UserBookId,
    Guid BookId,
    string Status,
    bool Wishlist,
    DateTime CreatedAt
);