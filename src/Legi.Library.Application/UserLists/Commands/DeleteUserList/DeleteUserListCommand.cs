using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.DeleteUserList;

public record DeleteUserListCommand(
    Guid ListId,
    Guid UserId
) : IRequest<Unit>;