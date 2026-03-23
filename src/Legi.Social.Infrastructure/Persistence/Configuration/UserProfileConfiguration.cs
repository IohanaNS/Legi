using Legi.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Social.Infrastructure.Persistence.Configuration;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        // PK is UserId, not a generated Id (UserProfile does not inherit BaseEntity)
        builder.HasKey(up => up.UserId);

        builder.Property(up => up.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever();

        builder.Property(up => up.Username)
            .HasColumnName("username")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(up => up.Bio)
            .HasColumnName("bio")
            .HasMaxLength(UserProfile.MaxBioLength);

        builder.Property(up => up.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(500);

        builder.Property(up => up.BannerUrl)
            .HasColumnName("banner_url")
            .HasMaxLength(500);

        builder.Property(up => up.FollowersCount)
            .HasColumnName("followers_count")
            .HasDefaultValue(0);

        builder.Property(up => up.FollowingCount)
            .HasColumnName("following_count")
            .HasDefaultValue(0);

        builder.Property(up => up.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(up => up.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
