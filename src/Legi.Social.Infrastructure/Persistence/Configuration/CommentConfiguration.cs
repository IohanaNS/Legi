using Legi.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Social.Infrastructure.Persistence.Configuration;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(c => c.TargetType)
            .HasColumnName("target_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.TargetId)
            .HasColumnName("target_id")
            .IsRequired();

        builder.Property(c => c.Content)
            .HasColumnName("content")
            .HasMaxLength(Comment.MaxContentLength)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Index for listing comments on content
        builder.HasIndex(c => new { c.TargetType, c.TargetId })
            .HasDatabaseName("ix_comments_target");

        // Index for paginated comment queries ordered by date
        builder.HasIndex(c => new { c.TargetType, c.TargetId, c.CreatedAt })
            .HasDatabaseName("ix_comments_target_created")
            .IsDescending(false, false, true);

        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("ix_comments_user_id");

        builder.Ignore(c => c.DomainEvents);
    }
}
