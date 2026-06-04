using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Catalog.Infrastructure.Persistence.Configurations;

public class BookRatingConfiguration : IEntityTypeConfiguration<BookRatingEntity>
{
    public void Configure(EntityTypeBuilder<BookRatingEntity> builder)
    {
        builder.ToTable("book_ratings");

        // Natural composite key: one rating per (book, user).
        builder.HasKey(br => new { br.BookId, br.UserId });

        builder.Property(br => br.BookId)
            .HasColumnName("book_id")
            .IsRequired();

        builder.Property(br => br.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(br => br.Rating)
            .HasColumnName("rating")
            .IsRequired();

        builder.Property(br => br.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Recompute reads all ratings for a book; the composite PK already leads
        // with book_id, so it serves the AVG/COUNT WHERE book_id = … lookup.
    }
}
