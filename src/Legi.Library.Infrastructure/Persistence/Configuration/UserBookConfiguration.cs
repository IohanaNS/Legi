using Legi.Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Library.Infrastructure.Persistence.Configuration;

public class UserBookConfiguration : IEntityTypeConfiguration<UserBook>
{
    public void Configure(EntityTypeBuilder<UserBook> builder)
    {
        builder.ToTable("user_books");

        builder.HasKey(ub => ub.Id);

        builder.Property(ub => ub.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(ub => ub.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ub => ub.BookId)
            .HasColumnName("book_id")
            .IsRequired();

        builder.Property(ub => ub.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ub => ub.WishList)
            .HasColumnName("wishlist")
            .HasDefaultValue(false);

        builder.Property(ub => ub.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(ub => ub.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ub => ub.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Value Object: Rating (owned, stored as column in same table)
        builder.OwnsOne(ub => ub.CurrentRating, rating =>
        {
            rating.Property(r => r.Value)
                .HasColumnName("rating_value")
                .HasColumnType("smallint");

            rating.Ignore(r => r.Stars); // Computed property, not persisted
        });

        // Value Object: Progress (owned, stored as columns in same table)
        builder.OwnsOne(ub => ub.CurrentProgress, progress =>
        {
            progress.Property(p => p.Value)
                .HasColumnName("progress_value");

            progress.Property(p => p.Type)
                .HasColumnName("progress_type")
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        // Global query filter: exclude soft-deleted records
        builder.HasQueryFilter(ub => ub.DeletedAt == null);

        // Unique index: one active UserBook per (user, book)
        builder.HasIndex(ub => new { ub.UserId, ub.BookId })
            .HasDatabaseName("ix_user_books_user_book")
            .HasFilter("deleted_at IS NULL")
            .IsUnique();

        // Index for user library queries
        builder.HasIndex(ub => ub.UserId)
            .HasDatabaseName("ix_user_books_user_id");

        // Ignore domain events collection
        builder.Ignore(ub => ub.DomainEvents);
        builder.Ignore(ub => ub.IsDeleted);
    }
}
