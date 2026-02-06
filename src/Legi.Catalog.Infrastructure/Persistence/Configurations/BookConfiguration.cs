using Legi.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Catalog.Infrastructure.Persistence.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Value Object: ISBN
        builder.OwnsOne(b => b.Isbn, isbn =>
        {
            isbn.Property(i => i.Value)
                .HasColumnName("isbn")
                .HasMaxLength(13)
                .IsRequired();

            isbn.HasIndex(i => i.Value)
                .IsUnique()
                .HasDatabaseName("ix_books_isbn");
        });

        builder.Property(b => b.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(b => b.Synopsis)
            .HasColumnName("synopsis")
            .HasColumnType("text");

        builder.Property(b => b.PageCount)
            .HasColumnName("page_count");

        builder.Property(b => b.Publisher)
            .HasColumnName("publisher")
            .HasMaxLength(255);

        builder.Property(b => b.CoverUrl)
            .HasColumnName("cover_url")
            .HasMaxLength(500);

        builder.Property(b => b.AverageRating)
            .HasColumnName("average_rating")
            .HasPrecision(3, 2)
            .HasDefaultValue(0);

        builder.Property(b => b.RatingsCount)
            .HasColumnName("ratings_count")
            .HasDefaultValue(0);

        builder.Property(b => b.ReviewsCount)
            .HasColumnName("reviews_count")
            .HasDefaultValue(0);

        builder.Property(b => b.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // IMPORTANT: Ignore the domain collections
        // These are managed separately via junction tables in the repository
        builder.Ignore(b => b.Tags);
        builder.Ignore(b => b.Authors);
        builder.Ignore(b => b.AuthorDisplay);

        // Indexes for common queries
        builder.HasIndex(b => b.Title)
            .HasDatabaseName("ix_books_title");

        builder.HasIndex(b => b.CreatedByUserId)
            .HasDatabaseName("ix_books_created_by_user_id");

        builder.HasIndex(b => b.AverageRating)
            .HasDatabaseName("ix_books_average_rating");
    }
}