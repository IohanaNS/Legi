using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Library.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DecoupleUserListItemFromUserBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // List items used to reference the owner's UserBook. They now reference the
            // catalog BookId directly so a list can contain books that are not in the
            // user's library. This is an add + backfill + drop so existing rows survive:
            // the previous column held UserBook IDs, which must be translated to BookIds.

            // The old unique index covered (user_list_id, user_book_id); drop it before
            // touching columns.
            migrationBuilder.DropIndex(
                name: "ix_user_list_items_list_book",
                table: "user_list_items");

            migrationBuilder.AddColumn<Guid>(
                name: "book_id",
                table: "user_list_items",
                type: "uuid",
                nullable: true);

            // Translate each item's UserBookId to the underlying BookId.
            migrationBuilder.Sql(@"
                UPDATE user_list_items AS i
                SET book_id = ub.book_id
                FROM user_books AS ub
                WHERE i.user_book_id = ub.id;");

            // Drop any items whose source UserBook no longer exists (cannot resolve a
            // BookId for them). Such rows would violate the NOT NULL constraint below.
            migrationBuilder.Sql("DELETE FROM user_list_items WHERE book_id IS NULL;");

            // Collapse duplicates that may now exist if a list referenced the same book
            // through two different UserBooks (re-added reading cycles): keep the lowest
            // order per (list, book).
            migrationBuilder.Sql(@"
                DELETE FROM user_list_items a
                USING user_list_items b
                WHERE a.user_list_id = b.user_list_id
                  AND a.book_id = b.book_id
                  AND a.""order"" > b.""order"";");

            migrationBuilder.AlterColumn<Guid>(
                name: "book_id",
                table: "user_list_items",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "user_book_id",
                table: "user_list_items");

            migrationBuilder.CreateIndex(
                name: "ix_user_list_items_list_book",
                table: "user_list_items",
                columns: new[] { "user_list_id", "book_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_list_items_list_book",
                table: "user_list_items");

            migrationBuilder.AddColumn<Guid>(
                name: "user_book_id",
                table: "user_list_items",
                type: "uuid",
                nullable: true);

            // Best-effort reverse: map each book back to one of the owner's UserBooks.
            // (A book can map to several UserBooks, so this is necessarily approximate.)
            migrationBuilder.Sql(@"
                UPDATE user_list_items AS i
                SET user_book_id = sub.id
                FROM (
                    SELECT ul.id AS list_id, uli.book_id, MIN(ub.id) AS id
                    FROM user_list_items uli
                    JOIN user_lists ul ON ul.id = uli.user_list_id
                    JOIN user_books ub ON ub.book_id = uli.book_id AND ub.user_id = ul.user_id
                    GROUP BY ul.id, uli.book_id
                ) AS sub
                WHERE i.user_list_id = sub.list_id AND i.book_id = sub.book_id;");

            migrationBuilder.Sql("DELETE FROM user_list_items WHERE user_book_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_book_id",
                table: "user_list_items",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "book_id",
                table: "user_list_items");

            migrationBuilder.CreateIndex(
                name: "ix_user_list_items_list_book",
                table: "user_list_items",
                columns: new[] { "user_list_id", "user_book_id" },
                unique: true);
        }
    }
}
