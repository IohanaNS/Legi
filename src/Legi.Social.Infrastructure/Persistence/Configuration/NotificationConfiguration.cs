using Legi.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Social.Infrastructure.Persistence.Configuration;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(n => n.RecipientId)
            .HasColumnName("recipient_id")
            .IsRequired();

        builder.Property(n => n.ActorId)
            .HasColumnName("actor_id")
            .IsRequired();

        builder.Property(n => n.ActorUsername)
            .HasColumnName("actor_username")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(n => n.ActorAvatarUrl)
            .HasColumnName("actor_avatar_url")
            .HasMaxLength(500);

        builder.Property(n => n.NotificationType)
            .HasColumnName("notification_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(n => n.TargetType)
            .HasColumnName("target_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(n => n.TargetId)
            .HasColumnName("target_id")
            .IsRequired();

        builder.Property(n => n.CommentPreview)
            .HasColumnName("comment_preview")
            .HasMaxLength(Notification.MaxCommentPreviewLength);

        builder.Property(n => n.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(n => n.ReadAt)
            .HasColumnName("read_at");

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // List query: a recipient's notifications, newest first.
        builder.HasIndex(n => new { n.RecipientId, n.CreatedAt })
            .HasDatabaseName("ix_notifications_recipient_created")
            .IsDescending(false, true);

        // Unread-count query.
        builder.HasIndex(n => new { n.RecipientId, n.IsRead })
            .HasDatabaseName("ix_notifications_recipient_is_read");
    }
}
