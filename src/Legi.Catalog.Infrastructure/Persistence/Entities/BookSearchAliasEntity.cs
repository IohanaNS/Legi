namespace Legi.Catalog.Infrastructure.Persistence.Entities;

/// <summary>
/// A normalized external-search query that resolved to a given book. Used to
/// widen local search so books are findable by terms that don't appear in their
/// stored title/ISBN (e.g. cross-language titles).
/// </summary>
public class BookSearchAliasEntity
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public string Alias { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
