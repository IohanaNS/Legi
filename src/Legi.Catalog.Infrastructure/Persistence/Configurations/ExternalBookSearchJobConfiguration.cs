using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Catalog.Infrastructure.Persistence.Configurations;

public class ExternalBookSearchJobConfiguration : IEntityTypeConfiguration<ExternalBookSearchJobEntity>
{
    public void Configure(EntityTypeBuilder<ExternalBookSearchJobEntity> builder)
    {
        builder.ToTable("external_book_search_jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(j => j.QueryHash)
            .HasColumnName("query_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(j => j.Query)
            .HasColumnName("query")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(j => j.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(j => j.RequestedByUserId)
            .HasColumnName("requested_by_user_id")
            .IsRequired();

        builder.Property(j => j.MaxResults)
            .HasColumnName("max_results")
            .IsRequired();

        builder.Property(j => j.Attempts)
            .HasColumnName("attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(j => j.NextRetryAt)
            .HasColumnName("next_retry_at")
            .IsRequired();

        builder.Property(j => j.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(j => j.StartedAt)
            .HasColumnName("started_at");

        builder.Property(j => j.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(j => j.CandidatesFound)
            .HasColumnName("candidates_found")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(j => j.ImportedCount)
            .HasColumnName("imported_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(j => j.UpdatedCount)
            .HasColumnName("updated_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(j => j.SkippedCount)
            .HasColumnName("skipped_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(j => j.Error)
            .HasColumnName("error")
            .HasColumnType("text");

        builder.HasIndex(j => j.QueryHash)
            .IsUnique()
            .HasFilter("\"status\" IN ('Pending', 'Processing')")
            .HasDatabaseName("ix_external_book_search_jobs_active_query_hash");

        builder.HasIndex(j => new { j.Status, j.NextRetryAt })
            .HasDatabaseName("ix_external_book_search_jobs_status_next_retry_at");

        builder.HasIndex(j => new { j.QueryHash, j.CompletedAt })
            .HasDatabaseName("ix_external_book_search_jobs_query_hash_completed_at");
    }
}
