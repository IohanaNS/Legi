using Legi.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260612144000_HashExistingRefreshTokens")]
    public partial class HashExistingRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                UPDATE refresh_tokens
                SET token_hash = encode(digest(token_hash, 'sha256'), 'base64')
                WHERE length(token_hash) > 44;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Refresh token hashes cannot be converted back to raw bearer tokens.");
        }
    }
}
