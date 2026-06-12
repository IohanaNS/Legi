using Legi.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Identity.Infrastructure.Persistence.Configurations;

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("login_attempts");

        builder.HasKey(la => la.Id);

        builder.Property(la => la.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(la => la.Identifier)
            .HasColumnName("identifier")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(la => la.FailedAttempts)
            .HasColumnName("failed_attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(la => la.LastFailedLoginAt)
            .HasColumnName("last_failed_login_at");

        builder.Property(la => la.LockoutEndsAt)
            .HasColumnName("lockout_ends_at");

        builder.Property(la => la.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(la => la.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(la => la.Identifier)
            .HasDatabaseName("ix_login_attempts_identifier")
            .IsUnique();
    }
}
