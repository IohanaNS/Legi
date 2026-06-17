namespace Legi.Catalog.Application.Books.DTOs;

/// <summary>
/// A single edition of a work, for the "other editions" list on a book detail
/// page. Edition-specific fields only.
/// </summary>
public record EditionSummaryDto(
    Guid Id,
    string Isbn,
    string Title,
    string? CoverUrl,
    string? Publisher,
    int? PageCount
);
