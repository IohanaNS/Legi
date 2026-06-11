using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.AddBookToList;

public class AddBookToListCommandHandler
    : IRequestHandler<AddBookToListCommand, AddBookToListResponse>
{
    private readonly IUserListRepository _userListRepository;
    private readonly IBookSnapshotRepository _bookSnapshotRepository;

    public AddBookToListCommandHandler(
        IUserListRepository userListRepository,
        IBookSnapshotRepository bookSnapshotRepository)
    {
        _userListRepository = userListRepository;
        _bookSnapshotRepository = bookSnapshotRepository;
    }

    public async Task<AddBookToListResponse> Handle(
        AddBookToListCommand request,
        CancellationToken cancellationToken)
    {
        var list = await _userListRepository.GetByIdAsync(
                       request.ListId, cancellationToken)
                   ?? throw new NotFoundException("UserList", request.ListId);

        if (list.UserId != request.UserId)
            throw new ForbiddenException();

        // Lists reference catalog books directly (not the user's library), so the
        // book only needs to be known to Library as a BookSnapshot.
        var snapshot = await _bookSnapshotRepository.GetByBookIdAsync(
            request.BookId, cancellationToken);
        if (snapshot is null)
            throw new NotFoundException("Book", request.BookId);

        list.AddBook(request.BookId);

        await _userListRepository.UpdateAsync(list, cancellationToken);

        return new AddBookToListResponse(
            list.Id,
            request.BookId,
            list.BooksCount);
    }
}
