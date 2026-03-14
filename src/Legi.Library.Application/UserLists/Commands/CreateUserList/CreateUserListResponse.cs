namespace Legi.Library.Application.UserLists.Commands.CreateUserList;

public record CreateUserListResponse(
    Guid ListId,
    string Name,
    string? Description,
    bool IsPublic,
    DateTime CreatedAt
);