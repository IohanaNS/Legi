using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Social.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileUsernameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_user_profiles_username",
                table: "user_profiles",
                column: "username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_profiles_username",
                table: "user_profiles");
        }
    }
}
