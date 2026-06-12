using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookWorkKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "work_key",
                table: "books",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            // Backfill pre-existing rows with a unique placeholder so legacy books
            // each stay their own "work" rather than collapsing into a shared ""
            // key. New rows always carry a real work key (resolved in Book.Create).
            migrationBuilder.Sql(
                "UPDATE books SET work_key = 'syn:legacy-' || id::text WHERE work_key = '';");

            migrationBuilder.CreateIndex(
                name: "ix_books_work_key",
                table: "books",
                column: "work_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_books_work_key",
                table: "books");

            migrationBuilder.DropColumn(
                name: "work_key",
                table: "books");
        }
    }
}
