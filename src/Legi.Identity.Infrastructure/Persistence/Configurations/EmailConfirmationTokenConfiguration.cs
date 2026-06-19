using Legi.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Identity.Infrastructure.Persistence.Configurations;

public class EmailConfirmationTokenConfiguration : IEntityTypeConfiguration<EmailConfirmationToken>
{
    public void Configure(EntityTypeBuilder<EmailConfirmationToken> builder)
    {
        builder.ToTable("email_confirmation_tokens");

        builder.HasKey(ect => ect.Id);

        builder.Property(ect => ect.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(ect => ect.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(ect => ect.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(ect => ect.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ect => ect.SentAt)
            .HasColumnName("sent_at");

        builder.Property(ect => ect.UsedAt)
            .HasColumnName("used_at");

        builder.Property<Guid>("UserId")
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(ect => ect.TokenHash)
            .HasDatabaseName("ix_email_confirmation_tokens_token_hash")
            .IsUnique();

        builder.HasIndex("UserId")
            .HasDatabaseName("ix_email_confirmation_tokens_user_id");
    }
}
