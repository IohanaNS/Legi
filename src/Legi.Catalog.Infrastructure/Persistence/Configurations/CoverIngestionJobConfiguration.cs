using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Catalog.Infrastructure.Persistence.Configurations;

public class CoverIngestionJobConfiguration : IEntityTypeConfiguration<CoverIngestionJobEntity>
{
    public void Configure(EntityTypeBuilder<CoverIngestionJobEntity> builder)
    {
        builder.ToTable("cover_ingestion_jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(j => j.BookId)
            .HasColumnName("book_id")
            .IsRequired();

        builder.Property(j => j.Isbn)
            .HasColumnName("isbn")
            .HasMaxLength(13)
            .IsRequired();

        builder.Property(j => j.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(j => j.NoCoverAttempts)
            .HasColumnName("no_cover_attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(j => j.TransientFailures)
            .HasColumnName("transient_failures")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(j => j.NextRetryAt)
            .HasColumnName("next_retry_at")
            .IsRequired();

        builder.Property(j => j.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(j => j.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(j => j.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(j => j.LastError)
            .HasColumnName("last_error")
            .HasColumnType("text");

        // At most one active discovery job per book (re-imports must not pile up
        // duplicate jobs); terminal rows are exempt so history is retained.
        builder.HasIndex(j => j.BookId)
            .IsUnique()
            .HasFilter("\"status\" IN ('Pending', 'Processing')")
            .HasDatabaseName("ix_cover_ingestion_jobs_active_book_id");

        // The worker's claim query orders Pending rows by due time.
        builder.HasIndex(j => new { j.Status, j.NextRetryAt })
            .HasDatabaseName("ix_cover_ingestion_jobs_status_next_retry_at");
    }
}
