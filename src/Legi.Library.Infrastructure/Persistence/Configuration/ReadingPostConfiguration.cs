using Legi.Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Library.Infrastructure.Persistence.Configurations;

public class ReadingPostConfiguration : IEntityTypeConfiguration<ReadingProgress>
{
    public void Configure(EntityTypeBuilder<ReadingProgress> builder)
    {
        builder.ToTable("reading_posts");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(rp => rp.UserBookId)
            .HasColumnName("user_book_id")
            .IsRequired();

        builder.Property(rp => rp.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(rp => rp.BookId)
            .HasColumnName("book_id")
            .IsRequired();

        builder.Property(rp => rp.Content)
            .HasColumnName("content")
            .HasMaxLength(ReadingProgress.MaxContentLength);

        builder.Property(rp => rp.ReadingDate)
            .HasColumnName("reading_date")
            .IsRequired();

        builder.Property(rp => rp.LikesCount)
            .HasColumnName("likes_count")
            .HasDefaultValue(0);

        builder.Property(rp => rp.CommentsCount)
            .HasColumnName("comments_count")
            .HasDefaultValue(0);

        builder.Property(rp => rp.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(rp => rp.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Value Object: Progress (owned, stored as columns in same table)
        builder.OwnsOne(rp => rp.CurrentProgress, progress =>
        {
            progress.Property(p => p.Value)
                .HasColumnName("progress_value");

            progress.Property(p => p.Type)
                .HasColumnName("progress_type")
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        // Indexes
        builder.HasIndex(rp => rp.UserBookId)
            .HasDatabaseName("ix_reading_posts_user_book_id");

        builder.HasIndex(rp => rp.UserId)
            .HasDatabaseName("ix_reading_posts_user_id");

        builder.HasIndex(rp => new { rp.UserBookId, rp.ReadingDate })
            .HasDatabaseName("ix_reading_posts_user_book_date")
            .IsDescending(false, true);

        // Ignore domain events
        builder.Ignore(rp => rp.DomainEvents);
    }
}
