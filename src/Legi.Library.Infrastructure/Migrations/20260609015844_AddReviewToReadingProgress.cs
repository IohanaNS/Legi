using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Library.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewToReadingProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_review",
                table: "reading_posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "rating_value",
                table: "reading_posts",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_review",
                table: "reading_posts");

            migrationBuilder.DropColumn(
                name: "rating_value",
                table: "reading_posts");
        }
    }
}
