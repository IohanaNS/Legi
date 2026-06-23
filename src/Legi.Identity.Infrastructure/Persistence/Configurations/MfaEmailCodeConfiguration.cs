using Legi.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Identity.Infrastructure.Persistence.Configurations;

public class MfaEmailCodeConfiguration : IEntityTypeConfiguration<MfaEmailCode>
{
    public void Configure(EntityTypeBuilder<MfaEmailCode> builder)
    {
        builder.ToTable("mfa_email_codes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(c => c.CodeHash)
            .HasColumnName("code_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(c => c.AttemptCount)
            .HasColumnName("attempt_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(c => c.ConsumedAt)
            .HasColumnName("consumed_at");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // At most one active code per user (the repository deletes the old one before issuing).
        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("ix_mfa_email_codes_user_id")
            .IsUnique();
    }
}
