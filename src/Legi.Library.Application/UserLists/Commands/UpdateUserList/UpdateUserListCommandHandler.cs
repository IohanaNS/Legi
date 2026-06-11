using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.UpdateUserList;

public class UpdateUserListCommandHandler
    : IRequestHandler<UpdateUserListCommand, UpdateUserListResponse>
{
    private readonly IUserListRepository _userListRepository;
    private readonly IBookSnapshotRepository _bookSnapshotRepository;

    public UpdateUserListCommandHandler(
        IUserListRepository userListRepository,
        IBookSnapshotRepository bookSnapshotRepository)
    {
        _userListRepository = userListRepository;
        _bookSnapshotRepository = bookSnapshotRepository;
    }

    public async Task<UpdateUserListResponse> Handle(
        UpdateUserListCommand request,
        CancellationToken cancellationToken)
    {
        var list = await _userListRepository.GetByIdAsync(
                       request.ListId, cancellationToken)
                   ?? throw new NotFoundException("UserList", request.ListId);

        if (list.UserId != request.UserId)
            throw new ForbiddenException();

        // Check name uniqueness only if name changed
        if (!string.Equals(list.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var nameExists = await _userListRepository.ExistsByUserAndNameAsync(
                request.UserId, request.Name, cancellationToken);

            if (nameExists)
                throw new ConflictException($"You already have a list named '{request.Name}'.");
        }

        // Validate any newly added books exist as a BookSnapshot.
        var existingBookIds = list.Items.Select(i => i.BookId).ToHashSet();
        foreach (var bookId in request.BookIds.Distinct().Where(id => !existingBookIds.Contains(id)))
        {
            var snapshot = await _bookSnapshotRepository.GetByBookIdAsync(bookId, cancellationToken);
            if (snapshot is null)
                throw new NotFoundException("Book", bookId);
        }

        list.UpdateDetails(request.Name, request.Description, request.IsPublic);
        list.SyncBooks(request.BookIds);

        await _userListRepository.UpdateAsync(list, cancellationToken);

        return new UpdateUserListResponse(
            list.Id,
            list.Name,
            list.Description,
            list.IsPublic,
            list.UpdatedAt);
    }
}
