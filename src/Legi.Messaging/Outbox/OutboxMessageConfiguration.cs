using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Messaging.Outbox;

/// <summary>
/// EF Core configuration for <see cref="OutboxMessage"/>. Applied by each
/// service's DbContext via <c>ApplyConfigurationsFromAssembly</c> or an
/// explicit <c>ApplyConfiguration</c> call. The schema is identical across
/// all services — each service just gets its own table in its own database.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, section 7.5.
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.Payload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(m => m.OccurredAt)
            .IsRequired();

        builder.Property(m => m.ProcessedAt);

        builder.Property(m => m.Attempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(m => m.Error);

        // Partial index optimized for the dispatcher's hot query:
        //   SELECT ... FROM outbox_messages
        //   WHERE processed_at IS NULL
        //   ORDER BY occurred_at LIMIT N FOR UPDATE SKIP LOCKED
        // The partial filter keeps the index small (only pending rows),
        // which matters once processed rows accumulate.
        builder.HasIndex(m => new { m.ProcessedAt, m.OccurredAt })
            .HasDatabaseName("ix_outbox_messages_pending")
            .HasFilter("\"ProcessedAt\" IS NULL");
    }
}