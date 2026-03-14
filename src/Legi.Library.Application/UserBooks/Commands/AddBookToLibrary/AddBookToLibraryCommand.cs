using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.AddBookToLibrary;

public record AddBookToLibraryCommand(
    Guid UserId,
    Guid BookId,
    bool Wishlist = false
) : IRequest<AddBookToLibraryResponse>;