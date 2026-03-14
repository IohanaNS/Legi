namespace Legi.Library.Application.Common.DTOs;

public record UserListSummaryDto(
    Guid ListId,
    string Name,
    string? Description,
    bool IsPublic,
    int BooksCount,
    int LikesCount,
    DateTime CreatedAt
);