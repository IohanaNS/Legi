using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.CreateUserList;

public class CreateUserListCommandHandler : IRequestHandler<CreateUserListCommand, CreateUserListResponse>
{
    private const int MaxListsPerUser = 100;
    private readonly IUserListRepository _userListRepository;
    private readonly IBookSnapshotRepository _bookSnapshotRepository;

    public CreateUserListCommandHandler(
        IUserListRepository userListRepository,
        IBookSnapshotRepository bookSnapshotRepository)
    {
        _userListRepository = userListRepository;
        _bookSnapshotRepository = bookSnapshotRepository;
    }

    public async Task<CreateUserListResponse> Handle(CreateUserListCommand request, CancellationToken cancellationToken)
    {
        var listCount = await _userListRepository.GetCountByUserIdAsync(
            request.UserId, cancellationToken);

        if (listCount >= MaxListsPerUser)
            throw new DomainException($"You cannot have more than {MaxListsPerUser} lists.");

        var nameExists = await _userListRepository.ExistsByUserAndNameAsync(
            request.UserId, request.Name, cancellationToken);

        if (nameExists)
            throw new ConflictException($"You already have a list named '{request.Name}'.");

        // Every book must already be projected as a BookSnapshot (same contract as
        // AddBookToLibrary). A missing snapshot means the book is unknown to Library.
        foreach (var bookId in request.BookIds.Distinct())
        {
            var snapshot = await _bookSnapshotRepository.GetByBookIdAsync(bookId, cancellationToken);
            if (snapshot is null)
                throw new NotFoundException("Book", bookId);
        }

        var list = UserList.Create(
            request.UserId,
            request.Name,
            request.Description,
            request.IsPublic);

        if (request.BookIds.Count > 0)
            list.SyncBooks(request.BookIds);

        await _userListRepository.AddAsync(list, cancellationToken);

        return new CreateUserListResponse(
            list.Id,
            list.Name,
            list.Description,
            list.IsPublic,
            list.CreatedAt);
    }
}
