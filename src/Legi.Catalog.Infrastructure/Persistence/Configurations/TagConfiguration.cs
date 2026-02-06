using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Catalog.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<TagEntity>
{
    public void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Slug)
            .HasColumnName("slug")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.UsageCount)
            .HasColumnName("usage_count")
            .HasDefaultValue(0);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Unique constraint on slug (normalized tag name)
        builder.HasIndex(t => t.Slug)
            .IsUnique()
            .HasDatabaseName("ix_tags_slug");

        // Index for autocomplete queries (search by name)
        builder.HasIndex(t => t.Name)
            .HasDatabaseName("ix_tags_name");

        // Index for sorting by popularity
        builder.HasIndex(t => t.UsageCount)
            .HasDatabaseName("ix_tags_usage_count");
    }
}