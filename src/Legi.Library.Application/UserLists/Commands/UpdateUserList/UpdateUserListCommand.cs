using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.UpdateUserList;

public record UpdateUserListCommand(
    Guid ListId,
    Guid UserId,
    string Name,
    string? Description,
    bool IsPublic
) : IRequest<UpdateUserListResponse>;