using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.RemoveBookFromLibrary;

public record RemoveBookFromLibraryCommand(
    Guid UserBookId,
    Guid UserId
) : IRequest<Unit>;