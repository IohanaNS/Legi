using Legi.Library.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Legi.Library.Infrastructure.Persistence.Configurations;

public class BookSnapshotConfiguration : IEntityTypeConfiguration<BookSnapshot>
{
    public void Configure(EntityTypeBuilder<BookSnapshot> builder)
    {
        builder.ToTable("book_snapshots");

        // PK is BookId, not a generated Id
        builder.HasKey(bs => bs.BookId);

        builder.Property(bs => bs.BookId)
            .HasColumnName("book_id")
            .ValueGeneratedNever();

        // Nullable: snapshots projected before the Work/Edition split have no
        // work id until Catalog re-projects (BookUpdated).
        builder.Property(bs => bs.WorkId)
            .HasColumnName("work_id");

        builder.HasIndex(bs => bs.WorkId)
            .HasDatabaseName("ix_book_snapshots_work_id");

        builder.Property(bs => bs.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(bs => bs.AuthorDisplay)
            .HasColumnName("author_display")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(bs => bs.CoverUrl)
            .HasColumnName("cover_url")
            .HasMaxLength(500);

        builder.Property(bs => bs.PageCount)
            .HasColumnName("page_count");

        builder.Property(bs => bs.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
