using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.RemoveBookFromLibrary;

public class RemoveBookFromLibraryCommandHandler
    : IRequestHandler<RemoveBookFromLibraryCommand, Unit>
{
    private readonly IUserBookRepository _userBookRepository;

    public RemoveBookFromLibraryCommandHandler(
        IUserBookRepository userBookRepository)
    {
        _userBookRepository = userBookRepository;
    }

    public async Task<Unit> Handle(
        RemoveBookFromLibraryCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load aggregate and verify ownership
        var userBook = await _userBookRepository.GetByIdAsync(
                           request.UserBookId, cancellationToken)
                       ?? throw new NotFoundException("UserBook", request.UserBookId);

        if (userBook.UserId != request.UserId)
            throw new ForbiddenException();

        // 2. Soft delete (emits BookRemovedFromLibraryDomainEvent).
        //    Lists are library-independent (they reference catalog BookIds), so
        //    removing a book from the library leaves any list memberships intact.
        userBook.Remove();

        // 3. Persist
        await _userBookRepository.UpdateAsync(userBook, cancellationToken);

        return Unit.Value;
    }
}
