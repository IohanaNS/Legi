using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalBookSearchJobResultCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "candidates_found",
                table: "external_book_search_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "imported_count",
                table: "external_book_search_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "skipped_count",
                table: "external_book_search_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "updated_count",
                table: "external_book_search_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "candidates_found",
                table: "external_book_search_jobs");

            migrationBuilder.DropColumn(
                name: "imported_count",
                table: "external_book_search_jobs");

            migrationBuilder.DropColumn(
                name: "skipped_count",
                table: "external_book_search_jobs");

            migrationBuilder.DropColumn(
                name: "updated_count",
                table: "external_book_search_jobs");
        }
    }
}
