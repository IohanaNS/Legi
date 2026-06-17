using Legi.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Catalog.Infrastructure.Persistence.Configurations;

public class WorkConfiguration : IEntityTypeConfiguration<Work>
{
    public void Configure(EntityTypeBuilder<Work> builder)
    {
        builder.ToTable("works");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Value Object: WorkKey — unique per work (one work per key).
        builder.OwnsOne(w => w.WorkKey, workKey =>
        {
            workKey.Property(k => k.Value)
                .HasColumnName("work_key")
                .HasMaxLength(255)
                .IsRequired();

            workKey.HasIndex(k => k.Value)
                .IsUnique()
                .HasDatabaseName("ix_works_work_key");
        });

        builder.Property(w => w.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(w => w.DefaultCoverUrl)
            .HasColumnName("default_cover_url")
            .HasMaxLength(500);

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
