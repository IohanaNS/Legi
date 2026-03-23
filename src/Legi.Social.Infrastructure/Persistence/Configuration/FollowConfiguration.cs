using Legi.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Social.Infrastructure.Persistence.Configuration;

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("follows");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(f => f.FollowerId)
            .HasColumnName("follower_id")
            .IsRequired();

        builder.Property(f => f.FollowingId)
            .HasColumnName("following_id")
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Unique constraint: one follow per (follower, following) pair
        builder.HasIndex(f => new { f.FollowerId, f.FollowingId })
            .HasDatabaseName("ix_follows_follower_following")
            .IsUnique();

        builder.HasIndex(f => f.FollowerId)
            .HasDatabaseName("ix_follows_follower_id");

        builder.HasIndex(f => f.FollowingId)
            .HasDatabaseName("ix_follows_following_id");

        builder.Ignore(f => f.DomainEvents);
    }
}
