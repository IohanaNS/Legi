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

        if (book == null) return book;
        await LoadAuthorsIntoDomainAsync(book, cancellationToken);
        await LoadTagsIntoDomainAsync(book, cancellationToken);

        return book;
    }

    public async Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        // Normalize the ISBN for comparison
        var normalizedIsbn = isbn.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();

        var book = await context.Books
            .FirstOrDefaultAsync(b => b.Isbn.Value == normalizedIsbn, cancellationToken);

        if (book == null) return book;
        await LoadAuthorsIntoDomainAsync(book, cancellationToken);
        await LoadTagsIntoDomainAsync(book, cancellationToken);

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
        await SynchronizeAuthorsAsync(book, cancellationToken);
        await SynchronizeTagsAsync(book, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        context.Books.Update(book);
        await SynchronizeAuthorsAsync(book, cancellationToken);
        await SynchronizeTagsAsync(book, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Book book, CancellationToken cancellationToken = default)
    {
        // Get existing relationships to update counts before deletion
        var bookAuthors = await context.BookAuthors
            .Include(ba => ba.Author)
            .Where(ba => ba.BookId == book.Id)
            .ToListAsync(cancellationToken);

        var bookTags = await context.BookTags
            .Include(bt => bt.Tag)
            .Where(bt => bt.BookId == book.Id)
            .ToListAsync(cancellationToken);

        // Decrement author book counts
        foreach (var bookAuthor in bookAuthors)
        {
            bookAuthor.Author.BooksCount = Math.Max(0, bookAuthor.Author.BooksCount - 1);
        }

        // Decrement tag usage counts
        foreach (var bookTag in bookTags)
        {
            bookTag.Tag.UsageCount = Math.Max(0, bookTag.Tag.UsageCount - 1);
        }

        // Delete the book (cascade will delete junction table entries)
        context.Books.Remove(book);
        await context.SaveChangesAsync(cancellationToken);
    }

    #region Author Synchronization

    /// <summary>
    /// Loads authors from the database and populates the domain Book's Authors collection.
    /// </summary>
    private async Task LoadAuthorsIntoDomainAsync(Book book, CancellationToken cancellationToken)
    {
        var bookAuthors = await context.BookAuthors
            .Include(ba => ba.Author)
            .Where(ba => ba.BookId == book.Id)
            .OrderBy(ba => ba.Order)
            .ToListAsync(cancellationToken);

        // Access the private _authors field via reflection
        var authorsField = typeof(Book).GetField("_authors",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (authorsField != null)
        {
            var authorsList = (List<Author>)authorsField.GetValue(book)!;
            authorsList.Clear();

            foreach (var bookAuthor in bookAuthors)
            {
                var author = Author.Create(bookAuthor.Author.Name);
                authorsList.Add(author);
            }
        }
    }

    /// <summary>
    /// Synchronizes the domain Book's Authors with the persistence layer.
    /// </summary>
    private async Task SynchronizeAuthorsAsync(Book book, CancellationToken cancellationToken)
    {
        var domainAuthors = book.Authors.ToList();

        // Get existing book-author relationships
        var existingBookAuthors = await context.BookAuthors
            .Where(ba => ba.BookId == book.Id)
            .Include(ba => ba.Author)
            .ToListAsync(cancellationToken);

        var existingSlugs = existingBookAuthors.Select(ba => ba.Author.Slug).ToHashSet();
        var domainSlugs = domainAuthors.Select(a => a.Slug).ToHashSet();

        // Remove authors that are no longer in the domain
        var authorsToRemove = existingBookAuthors
            .Where(ba => !domainSlugs.Contains(ba.Author.Slug))
            .ToList();

        foreach (var bookAuthor in authorsToRemove)
        {
            context.BookAuthors.Remove(bookAuthor);

            // Decrement books count
            bookAuthor.Author.BooksCount = Math.Max(0, bookAuthor.Author.BooksCount - 1);
        }

        // Add new authors and update order for existing ones
        for (var i = 0; i < domainAuthors.Count; i++)
        {
            var domainAuthor = domainAuthors[i];
            
            if (existingSlugs.Contains(domainAuthor.Slug))
            {
                // Update order if changed
                var existingBookAuthor = existingBookAuthors
                    .First(ba => ba.Author.Slug == domainAuthor.Slug);
                existingBookAuthor.Order = i;
            }
            else
            {
                // Find or create the AuthorEntity
                var authorEntity = await context.Authors
                    .FirstOrDefaultAsync(a => a.Slug == domainAuthor.Slug, cancellationToken);

                if (authorEntity == null)
                {
                    authorEntity = new AuthorEntity
                    {
                        Name = domainAuthor.Name,
                        Slug = domainAuthor.Slug,
                        BooksCount = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Authors.AddAsync(authorEntity, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken); // Save to get the Id
                }

                // Create the book-author relationship
                var bookAuthor = new BookAuthorEntity
                {
                    BookId = book.Id,
                    AuthorId = authorEntity.Id,
                    Order = i,
                    AddedAt = DateTime.UtcNow
                };
                await context.BookAuthors.AddAsync(bookAuthor, cancellationToken);

                // Increment books count
                authorEntity.BooksCount++;
            }
        }
    }

    #endregion

    #region Tag Synchronization

    /// <summary>
    /// Loads tags from the database and populates the domain Book's Tags collection.
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
                var tag = Tag.Create(bookTag.Tag.Name);
                tagsList.Add(tag);
            }
        }
    }

    /// <summary>
    /// Synchronizes the domain Book's Tags with the persistence layer.
    /// </summary>
    private async Task SynchronizeTagsAsync(Book book, CancellationToken cancellationToken)
    {
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

    #endregion
}