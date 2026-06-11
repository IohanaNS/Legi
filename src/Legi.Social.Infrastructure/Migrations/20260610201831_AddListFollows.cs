using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Social.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListFollows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "list_follows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_list_follows", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_list_follows_list_id",
                table: "list_follows",
                column: "list_id");

            migrationBuilder.CreateIndex(
                name: "ix_list_follows_user_list",
                table: "list_follows",
                columns: new[] { "user_id", "list_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "list_follows");
        }
    }
}
