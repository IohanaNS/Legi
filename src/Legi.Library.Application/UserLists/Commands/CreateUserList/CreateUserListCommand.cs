using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.CreateUserList;

public record CreateUserListCommand(
    Guid UserId,
    string Name,
    string? Description,
    bool IsPublic
) : IRequest<CreateUserListResponse>;