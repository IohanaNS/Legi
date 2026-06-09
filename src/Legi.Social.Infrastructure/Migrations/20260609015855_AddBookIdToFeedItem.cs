using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Social.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookIdToFeedItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "book_id",
                table: "feed_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_feed_items_book_activity_created",
                table: "feed_items",
                columns: new[] { "book_id", "activity_type", "created_at" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_feed_items_book_activity_created",
                table: "feed_items");

            migrationBuilder.DropColumn(
                name: "book_id",
                table: "feed_items");
        }
    }
}
