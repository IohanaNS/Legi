using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitWorkFromBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the works table.
            migrationBuilder.CreateTable(
                name: "works",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    default_cover_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_works", x => x.id);
                });

            // 2. Add the FK column nullable first so existing rows can be backfilled.
            migrationBuilder.AddColumn<Guid>(
                name: "work_id",
                table: "books",
                type: "uuid",
                nullable: true);

            // 3. Backfill: one work per distinct work_key (representative title +
            //    first available cover), then link each book to its work. Legacy
            //    books each have a unique work_key, so each becomes its own work
            //    (safe under-merge); future imports group by real work keys.
            migrationBuilder.Sql(
                """
                INSERT INTO works (id, work_key, title, default_cover_url, created_at, updated_at)
                SELECT gen_random_uuid(),
                       b.work_key,
                       (array_agg(b.title ORDER BY b.created_at))[1],
                       (array_agg(b.cover_url) FILTER (WHERE b.cover_url IS NOT NULL))[1],
                       now(),
                       now()
                FROM books b
                GROUP BY b.work_key;

                UPDATE books b SET work_id = w.id
                FROM works w
                WHERE b.work_key = w.work_key;
                """);

            // 4. Now every book has a work — enforce NOT NULL.
            migrationBuilder.AlterColumn<Guid>(
                name: "work_id",
                table: "books",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 5. Indexes + FK.
            migrationBuilder.CreateIndex(
                name: "ix_books_work_id",
                table: "books",
                column: "work_id");

            migrationBuilder.CreateIndex(
                name: "ix_works_work_key",
                table: "works",
                column: "work_key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_books_works_work_id",
                table: "books",
                column: "work_id",
                principalTable: "works",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_books_works_work_id",
                table: "books");

            migrationBuilder.DropTable(
                name: "works");

            migrationBuilder.DropIndex(
                name: "ix_books_work_id",
                table: "books");

            migrationBuilder.DropColumn(
                name: "work_id",
                table: "books");
        }
    }
}
