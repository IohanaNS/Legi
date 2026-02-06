namespace Legi.Catalog.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for the global tags' registry.
/// This is NOT a domain entity - it exists only for persistence and search/autocomplete.
/// The domain uses Tag as a Value Object within Book's aggregate.
/// </summary>
public class TagEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    
    /// <summary>
    /// Denormalized count for quick sorting by popularity.
    /// Updated via triggers or application logic when books add/remove tags.
    /// </summary>
    public int UsageCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation property for the many-to-many relationship
    public ICollection<BookTagEntity> BookTags { get; set; } = new List<BookTagEntity>();
}