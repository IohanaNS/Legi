using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Library.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBookWorkId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add nullable so existing rows can be backfilled.
            migrationBuilder.AddColumn<Guid>(
                name: "work_id",
                table: "user_books",
                type: "uuid",
                nullable: true);

            // 2. Backfill each user_book's work id from its book's snapshot
            //    (snapshots were themselves backfilled from Catalog). Hits all rows,
            //    including soft-deleted ones (no query filter in raw SQL).
            migrationBuilder.Sql(
                """
                UPDATE user_books ub
                SET work_id = bs.work_id
                FROM book_snapshots bs
                WHERE ub.book_id = bs.book_id AND bs.work_id IS NOT NULL;
                """);

            // 3. Enforce NOT NULL (all rows resolved — verified before migrating).
            migrationBuilder.AlterColumn<Guid>(
                name: "work_id",
                table: "user_books",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_books_work_id",
                table: "user_books",
                column: "work_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_books_work_id",
                table: "user_books");

            migrationBuilder.DropColumn(
                name: "work_id",
                table: "user_books");
        }
    }
}
