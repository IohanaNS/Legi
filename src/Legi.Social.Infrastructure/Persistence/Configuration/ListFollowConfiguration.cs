using Legi.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Social.Infrastructure.Persistence.Configuration;

public class ListFollowConfiguration : IEntityTypeConfiguration<ListFollow>
{
    public void Configure(EntityTypeBuilder<ListFollow> builder)
    {
        builder.ToTable("list_follows");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(f => f.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(f => f.ListId)
            .HasColumnName("list_id")
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // A user can follow a given list only once.
        builder.HasIndex(f => new { f.UserId, f.ListId })
            .HasDatabaseName("ix_list_follows_user_list")
            .IsUnique();

        // For counting followers of a list.
        builder.HasIndex(f => f.ListId)
            .HasDatabaseName("ix_list_follows_list_id");

        builder.Ignore(f => f.DomainEvents);
    }
}
