using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookSearchAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "book_search_aliases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alias = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_search_aliases", x => x.id);
                    table.ForeignKey(
                        name: "FK_book_search_aliases_books_book_id",
                        column: x => x.book_id,
                        principalTable: "books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_book_search_aliases_alias",
                table: "book_search_aliases",
                column: "alias");

            migrationBuilder.CreateIndex(
                name: "ix_book_search_aliases_book_id_alias",
                table: "book_search_aliases",
                columns: new[] { "book_id", "alias" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "book_search_aliases");
        }
    }
}
