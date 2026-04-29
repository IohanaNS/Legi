using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Messaging.Inbox;

/// <summary>
/// EF Core configuration for <see cref="InboxMessage"/>. Applied by each
/// service's DbContext. Schema is identical across services — each service
/// gets its own table in its own database.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, section 7.5.
/// </summary>
public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.ProcessedAt)
            .IsRequired();

        // No secondary indexes — every dispatch lookup is "WHERE Id = ?",
        // satisfied by the primary key.
    }
}