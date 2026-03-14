namespace Legi.Library.Application.Common.DTOs;

public record BookSnapshotDto(
    Guid BookId,
    string Title,
    string AuthorDisplay,
    string? CoverUrl,
    int? PageCount
);