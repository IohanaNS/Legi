namespace Legi.Library.Application.UserLists.Commands.UpdateUserList;

public record UpdateUserListResponse(
    Guid ListId,
    string Name,
    string? Description,
    bool IsPublic,
    DateTime UpdatedAt
);