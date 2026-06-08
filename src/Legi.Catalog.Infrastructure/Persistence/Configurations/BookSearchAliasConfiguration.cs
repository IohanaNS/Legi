using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Catalog.Infrastructure.Persistence.Configurations;

public class BookSearchAliasConfiguration : IEntityTypeConfiguration<BookSearchAliasEntity>
{
    public void Configure(EntityTypeBuilder<BookSearchAliasEntity> builder)
    {
        builder.ToTable("book_search_aliases");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.BookId)
            .HasColumnName("book_id")
            .IsRequired();

        builder.Property(a => a.Alias)
            .HasColumnName("alias")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(a => new { a.BookId, a.Alias })
            .IsUnique()
            .HasDatabaseName("ix_book_search_aliases_book_id_alias");

        builder.HasIndex(a => a.Alias)
            .HasDatabaseName("ix_book_search_aliases_alias");

        // Aliases are a derived search index — drop them with their book.
        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(a => a.BookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
