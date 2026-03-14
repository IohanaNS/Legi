namespace Legi.Library.Application.Common.DTOs;

public record UserListDetailDto(
    Guid ListId,
    Guid UserId,
    string Name,
    string? Description,
    bool IsPublic,
    int BooksCount,
    int LikesCount,
    int CommentsCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);