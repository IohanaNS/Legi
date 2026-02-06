using Legi.Catalog.Domain.Entities;

namespace Legi.Catalog.Infrastructure.Persistence.Entities;

/// <summary>
/// Junction entity for the many-to-many relationship between Books and Tags.
/// </summary>
public class BookTagEntity
{
    public Guid BookId { get; set; }
    public int TagId { get; set; }
    
    public DateTime AddedAt { get; set; }
    
    // Navigation properties
    public Book Book { get; set; } = null!;
    public TagEntity Tag { get; set; } = null!;
}