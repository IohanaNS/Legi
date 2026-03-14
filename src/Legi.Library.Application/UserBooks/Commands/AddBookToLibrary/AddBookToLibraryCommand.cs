using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.AddBookToLibrary;

public record AddBookToLibraryCommand(
    Guid UserId,
    Guid BookId,
    bool Wishlist = false,
    string? BookTitle = null,
    string? BookAuthorDisplay = null,
    string? BookCoverUrl = null,
    int? BookPageCount = null
) : IRequest<AddBookToLibraryResponse>;