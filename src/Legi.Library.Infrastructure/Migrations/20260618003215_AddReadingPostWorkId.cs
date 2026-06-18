using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Library.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingPostWorkId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Nullable first for backfill.
            migrationBuilder.AddColumn<Guid>(
                name: "work_id",
                table: "reading_posts",
                type: "uuid",
                nullable: true);

            // 2. Backfill each post's work from its book's snapshot.
            migrationBuilder.Sql(
                """
                UPDATE reading_posts rp
                SET work_id = bs.work_id
                FROM book_snapshots bs
                WHERE rp.book_id = bs.book_id AND bs.work_id IS NOT NULL;
                """);

            // 3. Orphan legacy posts (book has no snapshot / no work) get a unique
            //    placeholder work so they stay non-null and don't aggregate with
            //    anything. New posts always carry a real work id.
            migrationBuilder.Sql(
                "UPDATE reading_posts SET work_id = gen_random_uuid() WHERE work_id IS NULL;");

            // 4. Enforce NOT NULL.
            migrationBuilder.AlterColumn<Guid>(
                name: "work_id",
                table: "reading_posts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_reading_posts_work_id",
                table: "reading_posts",
                column: "work_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_reading_posts_work_id",
                table: "reading_posts");

            migrationBuilder.DropColumn(
                name: "work_id",
                table: "reading_posts");
        }
    }
}
