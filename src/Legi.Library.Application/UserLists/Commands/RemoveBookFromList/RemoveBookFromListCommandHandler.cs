using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.RemoveBookFromList;

public class RemoveBookFromListCommandHandler
    : IRequestHandler<RemoveBookFromListCommand, Unit>
{
    private readonly IUserListRepository _userListRepository;

    public RemoveBookFromListCommandHandler(IUserListRepository userListRepository)
    {
        _userListRepository = userListRepository;
    }

    public async Task<Unit> Handle(
        RemoveBookFromListCommand request,
        CancellationToken cancellationToken)
    {
        var list = await _userListRepository.GetByIdAsync(
                       request.ListId, cancellationToken)
                   ?? throw new NotFoundException("UserList", request.ListId);

        if (list.UserId != request.UserId)
            throw new ForbiddenException();

        list.RemoveBook(request.UserBookId); // Throws if not found

        await _userListRepository.UpdateAsync(list, cancellationToken);

        return Unit.Value;
    }
}