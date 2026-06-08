using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Library.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingPostIsSpoiler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_spoiler",
                table: "reading_posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_spoiler",
                table: "reading_posts");
        }
    }
}
