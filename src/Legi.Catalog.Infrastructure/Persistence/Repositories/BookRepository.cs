using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;
using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure.Persistence.Repositories;

public class BookRepository(CatalogDbContext context) : IBookRepository
{
    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await context.Books
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (book != null)
        {
            await LoadTagsIntoDomainAsync(book, cancellationToken);
        }

        return book;
    }

    public async Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        // Normalize the ISBN for comparison
        var normalizedIsbn = isbn.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();

        var book = await context.Books
            .FirstOrDefaultAsync(b => b.Isbn.Value == normalizedIsbn, cancellationToken);

        if (book != null)
        {
            await LoadTagsIntoDomainAsync(book, cancellationToken);
        }

        return book;
    }

    public async Task<bool> ExistsByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        var normalizedIsbn = isbn.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();

        return await context.Books
            .AnyAsync(b => b.Isbn.Value == normalizedIsbn, cancellationToken);
    }

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        await context.Books.AddAsync(book, cancellationToken);
        await SynchronizeTagsAsync(book, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        context.Books.Update(book);
        await SynchronizeTagsAsync(book, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Loads tags from the database and populates the domain Book's Tags collection.
    /// Uses reflection to access the private _tags field since Tags is read-only.
    /// </summary>
    private async Task LoadTagsIntoDomainAsync(Book book, CancellationToken cancellationToken)
    {
        var bookTags = await context.BookTags
            .Include(bt => bt.Tag)
            .Where(bt => bt.BookId == book.Id)
            .ToListAsync(cancellationToken);

        // Access the private _tags field via reflection
        var tagsField = typeof(Book).GetField("_tags", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (tagsField != null)
        {
            var tagsList = (List<Tag>)tagsField.GetValue(book)!;
            tagsList.Clear();

            foreach (var bookTag in bookTags)
            {
                // Recreate the domain Tag Value Object from persistence data
                var tag = Tag.Create(bookTag.Tag.Name);
                tagsList.Add(tag);
            }
        }
    }

    /// <summary>
    /// Synchronizes the domain Book's Tags with the persistence layer.
    /// This handles creating new tags, linking existing tags, and removing old links.
    /// </summary>
    private async Task SynchronizeTagsAsync(Book book, CancellationToken cancellationToken)
    {
        // Get current tags from domain
        var domainTags = book.Tags.ToList();

        // Get existing book-tag relationships
        var existingBookTags = await context.BookTags
            .Where(bt => bt.BookId == book.Id)
            .Include(bt => bt.Tag)
            .ToListAsync(cancellationToken);

        var existingSlugs = existingBookTags.Select(bt => bt.Tag.Slug).ToHashSet();
        var domainSlugs = domainTags.Select(t => t.Slug).ToHashSet();

        // Remove tags that are no longer in the domain
        var tagsToRemove = existingBookTags
            .Where(bt => !domainSlugs.Contains(bt.Tag.Slug))
            .ToList();

        foreach (var bookTag in tagsToRemove)
        {
            context.BookTags.Remove(bookTag);
            
            // Decrement usage count
            bookTag.Tag.UsageCount = Math.Max(0, bookTag.Tag.UsageCount - 1);
        }

        // Add new tags
        var tagsToAdd = domainTags
            .Where(t => !existingSlugs.Contains(t.Slug))
            .ToList();

        foreach (var domainTag in tagsToAdd)
        {
            // Find or create the TagEntity
            var tagEntity = await context.Tags
                .FirstOrDefaultAsync(t => t.Slug == domainTag.Slug, cancellationToken);

            if (tagEntity == null)
            {
                // Create new tag in the global registry
                tagEntity = new TagEntity
                {
                    Name = domainTag.Name,
                    Slug = domainTag.Slug,
                    UsageCount = 0,
                    CreatedAt = DateTime.UtcNow
                };
                await context.Tags.AddAsync(tagEntity, cancellationToken);
                await context.SaveChangesAsync(cancellationToken); // Save to get the Id
            }

            // Create the book-tag relationship
            var bookTag = new BookTagEntity
            {
                BookId = book.Id,
                TagId = tagEntity.Id,
                AddedAt = DateTime.UtcNow
            };
            await context.BookTags.AddAsync(bookTag, cancellationToken);

            // Increment usage count
            tagEntity.UsageCount++;
        }
    }
}