using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Library.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBookFinishedReadingAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "finished_reading_at",
                table: "user_books",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "finished_reading_at",
                table: "user_books");
        }
    }
}
