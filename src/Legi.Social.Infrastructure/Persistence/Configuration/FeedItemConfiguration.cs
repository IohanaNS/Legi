using Legi.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Social.Infrastructure.Persistence.Configuration;

public class FeedItemConfiguration : IEntityTypeConfiguration<FeedItem>
{
    public void Configure(EntityTypeBuilder<FeedItem> builder)
    {
        builder.ToTable("feed_items");

        builder.HasKey(fi => fi.Id);

        builder.Property(fi => fi.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(fi => fi.ActorId)
            .HasColumnName("actor_id")
            .IsRequired();

        builder.Property(fi => fi.ActorUsername)
            .HasColumnName("actor_username")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(fi => fi.ActorAvatarUrl)
            .HasColumnName("actor_avatar_url")
            .HasMaxLength(500);

        builder.Property(fi => fi.ActivityType)
            .HasColumnName("activity_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(fi => fi.TargetType)
            .HasColumnName("target_type")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(fi => fi.ReferenceId)
            .HasColumnName("reference_id")
            .IsRequired();

        builder.Property(fi => fi.BookTitle)
            .HasColumnName("book_title")
            .HasMaxLength(500);

        builder.Property(fi => fi.BookAuthor)
            .HasColumnName("book_author")
            .HasMaxLength(500);

        builder.Property(fi => fi.BookCoverUrl)
            .HasColumnName("book_cover_url")
            .HasMaxLength(500);

        builder.Property(fi => fi.Data)
            .HasColumnName("data")
            .HasColumnType("jsonb");

        builder.Property(fi => fi.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(fi => fi.ActorId)
            .HasDatabaseName("ix_feed_items_actor_id");

        builder.HasIndex(fi => fi.CreatedAt)
            .HasDatabaseName("ix_feed_items_created_at")
            .IsDescending();

        builder.HasIndex(fi => fi.ReferenceId)
            .HasDatabaseName("ix_feed_items_reference_id");
    }
}
