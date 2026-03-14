using Legi.Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Library.Infrastructure.Persistence.Configurations;

public class UserListItemConfiguration : IEntityTypeConfiguration<UserListItem>
{
    public void Configure(EntityTypeBuilder<UserListItem> builder)
    {
        builder.ToTable("user_list_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(i => i.UserBookId)
            .HasColumnName("user_book_id")
            .IsRequired();

        builder.Property(i => i.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(i => i.AddedAt)
            .HasColumnName("added_at")
            .IsRequired();

        // Shadow property for FK (set in UserListConfiguration)
        builder.Property<Guid>("user_list_id")
            .IsRequired();

        // No duplicate books in same list
        builder.HasIndex("user_list_id", nameof(UserListItem.UserBookId))
            .HasDatabaseName("ix_user_list_items_list_book")
            .IsUnique();

        // For ordering within a list
        builder.HasIndex("user_list_id", nameof(UserListItem.Order))
            .HasDatabaseName("ix_user_list_items_list_order");

        // Ignore domain events
        builder.Ignore(i => i.DomainEvents);
    }
}
