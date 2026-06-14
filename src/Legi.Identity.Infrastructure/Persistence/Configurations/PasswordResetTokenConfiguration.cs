using Legi.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Identity.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");

        builder.HasKey(prt => prt.Id);

        builder.Property(prt => prt.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(prt => prt.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(prt => prt.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(prt => prt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(prt => prt.UsedAt)
            .HasColumnName("used_at");

        // Shadow property for FK (not exposed to the entity)
        builder.Property<Guid>("UserId")
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(prt => prt.TokenHash)
            .HasDatabaseName("ix_password_reset_tokens_token_hash")
            .IsUnique();

        builder.HasIndex("UserId")
            .HasDatabaseName("ix_password_reset_tokens_user_id");
    }
}
