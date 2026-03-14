using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.DeleteUserList;

public class DeleteUserListCommandHandler
    : IRequestHandler<DeleteUserListCommand, Unit>
{
    private readonly IUserListRepository _userListRepository;

    public DeleteUserListCommandHandler(IUserListRepository userListRepository)
    {
        _userListRepository = userListRepository;
    }

    public async Task<Unit> Handle(
        DeleteUserListCommand request,
        CancellationToken cancellationToken)
    {
        var list = await _userListRepository.GetByIdAsync(
                       request.ListId, cancellationToken)
                   ?? throw new NotFoundException("UserList", request.ListId);

        if (list.UserId != request.UserId)
            throw new ForbiddenException();

        list.Delete(); // Emits UserListDeletedDomainEvent

        await _userListRepository.DeleteAsync(list, cancellationToken);

        return Unit.Value;
    }
}