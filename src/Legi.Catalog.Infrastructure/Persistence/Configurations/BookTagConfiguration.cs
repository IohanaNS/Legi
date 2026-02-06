using Legi.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Catalog.Infrastructure.Persistence.Configurations;

public class BookTagConfiguration : IEntityTypeConfiguration<BookTagEntity>
{
    public void Configure(EntityTypeBuilder<BookTagEntity> builder)
    {
        builder.ToTable("book_tags");

        // Composite primary key
        builder.HasKey(bt => new { bt.BookId, bt.TagId });

        builder.Property(bt => bt.BookId)
            .HasColumnName("book_id");

        builder.Property(bt => bt.TagId)
            .HasColumnName("tag_id");

        builder.Property(bt => bt.AddedAt)
            .HasColumnName("added_at")
            .IsRequired();

        // Relationships
        builder.HasOne(bt => bt.Book)
            .WithMany()
            .HasForeignKey(bt => bt.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bt => bt.Tag)
            .WithMany(t => t.BookTags)
            .HasForeignKey(bt => bt.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for querying books by tag
        builder.HasIndex(bt => bt.TagId)
            .HasDatabaseName("ix_book_tags_tag_id");
    }
}