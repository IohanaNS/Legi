using Legi.Catalog.Domain.Entities;

namespace Legi.Catalog.Infrastructure.Persistence.Entities;

/// <summary>
/// Junction entity for the many-to-many relationship between Books and Authors.
/// </summary>
public class BookAuthorEntity
{
    public Guid BookId { get; set; }
    public int AuthorId { get; set; }

    /// <summary>
    /// Order of the author in the book (primary author = 0, etc.)
    /// </summary>
    public int Order { get; set; }

    public DateTime AddedAt { get; set; }

    // Navigation properties
    public Book Book { get; set; } = null!;
    public AuthorEntity Author { get; set; } = null!;
}