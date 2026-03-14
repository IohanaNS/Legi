using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Library.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialLibrarySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "book_snapshots",
                columns: table => new
                {
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    author_display = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    cover_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_snapshots", x => x.book_id);
                });

            migrationBuilder.CreateTable(
                name: "reading_posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    progress_value = table.Column<int>(type: "integer", nullable: true),
                    progress_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    likes_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    comments_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    reading_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reading_posts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_books",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    progress_value = table.Column<int>(type: "integer", nullable: true),
                    progress_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    wishlist = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    rating_value = table.Column<short>(type: "smallint", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_books", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    likes_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    comments_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    books_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_list_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_list_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_list_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_list_items_user_lists_user_list_id",
                        column: x => x.user_list_id,
                        principalTable: "user_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reading_posts_user_book_date",
                table: "reading_posts",
                columns: new[] { "user_book_id", "reading_date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_reading_posts_user_book_id",
                table: "reading_posts",
                column: "user_book_id");

            migrationBuilder.CreateIndex(
                name: "ix_reading_posts_user_id",
                table: "reading_posts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_books_user_book",
                table: "user_books",
                columns: new[] { "user_id", "book_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_books_user_id",
                table: "user_books",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_list_items_list_book",
                table: "user_list_items",
                columns: new[] { "user_list_id", "user_book_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_list_items_list_order",
                table: "user_list_items",
                columns: new[] { "user_list_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_user_lists_is_public",
                table: "user_lists",
                column: "is_public",
                filter: "is_public = true");

            migrationBuilder.CreateIndex(
                name: "ix_user_lists_user_id",
                table: "user_lists",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_lists_user_name",
                table: "user_lists",
                columns: new[] { "user_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "book_snapshots");

            migrationBuilder.DropTable(
                name: "reading_posts");

            migrationBuilder.DropTable(
                name: "user_books");

            migrationBuilder.DropTable(
                name: "user_list_items");

            migrationBuilder.DropTable(
                name: "user_lists");
        }
    }
}
