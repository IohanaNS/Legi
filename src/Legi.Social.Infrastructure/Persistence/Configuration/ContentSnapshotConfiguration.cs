using Legi.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Social.Infrastructure.Persistence.Configuration;

public class ContentSnapshotConfiguration : IEntityTypeConfiguration<ContentSnapshot>
{
    public void Configure(EntityTypeBuilder<ContentSnapshot> builder)
    {
        builder.ToTable("content_snapshots");

        // Composite PK: (TargetType, TargetId)
        builder.HasKey(cs => new { cs.TargetType, cs.TargetId });

        builder.Property(cs => cs.TargetType)
            .HasColumnName("target_type")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(cs => cs.TargetId)
            .HasColumnName("target_id");

        builder.Property(cs => cs.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(cs => cs.OwnerUsername)
            .HasColumnName("owner_username")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(cs => cs.OwnerAvatarUrl)
            .HasColumnName("owner_avatar_url")
            .HasMaxLength(500);

        builder.Property(cs => cs.BookTitle)
            .HasColumnName("book_title")
            .HasMaxLength(500);

        builder.Property(cs => cs.BookAuthor)
            .HasColumnName("book_author")
            .HasMaxLength(500);

        builder.Property(cs => cs.BookCoverUrl)
            .HasColumnName("book_cover_url")
            .HasMaxLength(500);

        builder.Property(cs => cs.ContentPreview)
            .HasColumnName("content_preview")
            .HasMaxLength(ContentSnapshot.MaxContentPreviewLength);

        builder.Property(cs => cs.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(cs => cs.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
