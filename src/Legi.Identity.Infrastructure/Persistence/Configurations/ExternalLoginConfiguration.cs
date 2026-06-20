using Legi.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Identity.Infrastructure.Persistence.Configurations;

public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("external_logins");

        builder.HasKey(el => el.Id);

        builder.Property(el => el.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(el => el.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(el => el.ProviderKey)
            .HasColumnName("provider_key")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(el => el.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Shadow property for FK (not exposed to the entity)
        builder.Property<Guid>("UserId")
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(el => new { el.Provider, el.ProviderKey })
            .HasDatabaseName("ix_external_logins_provider_provider_key")
            .IsUnique();

        builder.HasIndex("UserId")
            .HasDatabaseName("ix_external_logins_user_id");
    }
}
