using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Catalog.Infrastructure.Persistence.Configurations;

public class AuthorConfiguration : IEntityTypeConfiguration<AuthorEntity>
{
    public void Configure(EntityTypeBuilder<AuthorEntity> builder)
    {
        builder.ToTable("authors");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.Slug)
            .HasColumnName("slug")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.BooksCount)
            .HasColumnName("books_count")
            .HasDefaultValue(0);

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Unique constraint on slug
        builder.HasIndex(a => a.Slug)
            .IsUnique()
            .HasDatabaseName("ix_authors_slug");

        // Index for autocomplete queries
        builder.HasIndex(a => a.Name)
            .HasDatabaseName("ix_authors_name");

        // Index for sorting by popularity
        builder.HasIndex(a => a.BooksCount)
            .HasDatabaseName("ix_authors_books_count");
    }
}