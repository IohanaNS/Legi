using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Catalog.Infrastructure.Persistence.Configurations;

public class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthorEntity>
{
    public void Configure(EntityTypeBuilder<BookAuthorEntity> builder)
    {
        builder.ToTable("book_authors");

        // Composite primary key
        builder.HasKey(ba => new { ba.BookId, ba.AuthorId });

        builder.Property(ba => ba.BookId)
            .HasColumnName("book_id");

        builder.Property(ba => ba.AuthorId)
            .HasColumnName("author_id");

        builder.Property(ba => ba.Order)
            .HasColumnName("order")
            .HasDefaultValue(0);

        builder.Property(ba => ba.AddedAt)
            .HasColumnName("added_at")
            .IsRequired();

        // Relationships
        builder.HasOne(ba => ba.Book)
            .WithMany()
            .HasForeignKey(ba => ba.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ba => ba.Author)
            .WithMany(a => a.BookAuthors)
            .HasForeignKey(ba => ba.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for querying books by author
        builder.HasIndex(ba => ba.AuthorId)
            .HasDatabaseName("ix_book_authors_author_id");

        // Index for ordering authors within a book
        builder.HasIndex(ba => new { ba.BookId, ba.Order })
            .HasDatabaseName("ix_book_authors_book_order");
    }
}