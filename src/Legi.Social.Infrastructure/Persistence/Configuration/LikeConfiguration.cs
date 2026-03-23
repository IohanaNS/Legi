using Legi.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Social.Infrastructure.Persistence.Configuration;

public class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.ToTable("likes");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(l => l.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(l => l.TargetType)
            .HasColumnName("target_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.TargetId)
            .HasColumnName("target_id")
            .IsRequired();

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Unique constraint: user can like same content only once
        builder.HasIndex(l => new { l.UserId, l.TargetType, l.TargetId })
            .HasDatabaseName("ix_likes_user_target")
            .IsUnique();

        // Index for counting likes on content
        builder.HasIndex(l => new { l.TargetType, l.TargetId })
            .HasDatabaseName("ix_likes_target");

        builder.HasIndex(l => l.UserId)
            .HasDatabaseName("ix_likes_user_id");

        builder.Ignore(l => l.DomainEvents);
    }
}
