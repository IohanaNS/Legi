using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentifierLoginAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "login_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identifier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_failed_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lockout_ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_attempts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_login_attempts_identifier",
                table: "login_attempts",
                column: "identifier",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO login_attempts (
                    id,
                    identifier,
                    failed_attempts,
                    last_failed_login_at,
                    lockout_ends_at,
                    created_at,
                    updated_at)
                SELECT
                    md5('email:' || email)::uuid,
                    email,
                    failed_login_attempts,
                    last_failed_login_at,
                    login_lockout_ends_at,
                    NOW(),
                    NOW()
                FROM users
                WHERE failed_login_attempts > 0
                   OR login_lockout_ends_at IS NOT NULL
                ON CONFLICT (identifier) DO NOTHING;

                INSERT INTO login_attempts (
                    id,
                    identifier,
                    failed_attempts,
                    last_failed_login_at,
                    lockout_ends_at,
                    created_at,
                    updated_at)
                SELECT
                    md5('username:' || username)::uuid,
                    username,
                    failed_login_attempts,
                    last_failed_login_at,
                    login_lockout_ends_at,
                    NOW(),
                    NOW()
                FROM users
                WHERE failed_login_attempts > 0
                   OR login_lockout_ends_at IS NOT NULL
                ON CONFLICT (identifier) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "login_attempts");
        }
    }
}
