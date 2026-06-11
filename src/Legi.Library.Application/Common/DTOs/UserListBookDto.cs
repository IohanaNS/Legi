namespace Legi.Library.Application.Common.DTOs;

public record UserListBookDto(
    Guid BookId,
    int Order,
    BookSnapshotDto Book,
    DateTime AddedAt
);
