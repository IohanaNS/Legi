using Legi.Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Library.Infrastructure.Persistence.Configurations;

public class UserListConfiguration : IEntityTypeConfiguration<UserList>
{
    public void Configure(EntityTypeBuilder<UserList> builder)
    {
        builder.ToTable("user_lists");

        builder.HasKey(ul => ul.Id);

        builder.Property(ul => ul.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(ul => ul.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ul => ul.Name)
            .HasColumnName("name")
            .HasMaxLength(UserList.MaxNameLength)
            .IsRequired();

        builder.Property(ul => ul.Description)
            .HasColumnName("description")
            .HasMaxLength(UserList.MaxDescriptionLength);

        builder.Property(ul => ul.IsPublic)
            .HasColumnName("is_public")
            .HasDefaultValue(false);

        builder.Property(ul => ul.BooksCount)
            .HasColumnName("books_count")
            .HasDefaultValue(0);

        builder.Property(ul => ul.LikesCount)
            .HasColumnName("likes_count")
            .HasDefaultValue(0);

        builder.Property(ul => ul.CommentsCount)
            .HasColumnName("comments_count")
            .HasDefaultValue(0);

        builder.Property(ul => ul.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ul => ul.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Relationship: UserList owns UserListItems
        builder.HasMany(ul => ul.Items)
            .WithOne()
            .HasForeignKey("user_list_id")
            .OnDelete(DeleteBehavior.Cascade);

        // Access private backing field for Items
        builder.Navigation(ul => ul.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(ul => ul.UserId)
            .HasDatabaseName("ix_user_lists_user_id");

        // Unique name per user (case-insensitive via database collation)
        builder.HasIndex(ul => new { ul.UserId, ul.Name })
            .HasDatabaseName("ix_user_lists_user_name")
            .IsUnique();

        // For public list search
        builder.HasIndex(ul => ul.IsPublic)
            .HasDatabaseName("ix_user_lists_is_public")
            .HasFilter("is_public = true");

        // Ignore domain events
        builder.Ignore(ul => ul.DomainEvents);
    }
}
