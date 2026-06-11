namespace Legi.Library.Application.Common.DTOs;

public record UserListSummaryDto(
    Guid ListId,
    Guid OwnerId,
    string Name,
    string? Description,
    bool IsPublic,
    int BooksCount,
    int LikesCount,
    DateTime CreatedAt,
    IReadOnlyList<BookSnapshotDto> PreviewBooks
);
