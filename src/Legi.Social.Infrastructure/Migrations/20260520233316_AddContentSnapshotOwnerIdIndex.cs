using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Social.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentSnapshotOwnerIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_content_snapshots_owner_id",
                table: "content_snapshots",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_content_snapshots_owner_id",
                table: "content_snapshots");
        }
    }
}
