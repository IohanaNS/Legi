using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.RemoveBookFromLibrary;

public class RemoveBookFromLibraryCommandHandler
    : IRequestHandler<RemoveBookFromLibraryCommand, Unit>
{
    private readonly IUserBookRepository _userBookRepository;
    private readonly IUserListRepository _userListRepository;

    public RemoveBookFromLibraryCommandHandler(
        IUserBookRepository userBookRepository,
        IUserListRepository userListRepository)
    {
        _userBookRepository = userBookRepository;
        _userListRepository = userListRepository;
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

        // 2. Soft delete (emits BookRemovedFromLibraryDomainEvent)
        userBook.Remove();

        // 3. Clean up list references (hard delete UserListItems)
        var listsWithBook = await _userListRepository.GetListsContainingBookAsync(
            userBook.Id, cancellationToken);

        foreach (var list in listsWithBook)
        {
            list.RemoveBookIfExists(userBook.Id);
            await _userListRepository.UpdateAsync(list, cancellationToken);
        }

        // 4. Persist
        await _userBookRepository.UpdateAsync(userBook, cancellationToken);

        return Unit.Value;
    }
}