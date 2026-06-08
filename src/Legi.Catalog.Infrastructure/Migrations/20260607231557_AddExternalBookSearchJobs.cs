using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalBookSearchJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_book_search_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    query_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    query = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_results = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_book_search_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_external_book_search_jobs_active_query_hash",
                table: "external_book_search_jobs",
                column: "query_hash",
                unique: true,
                filter: "\"status\" IN ('Pending', 'Processing')");

            migrationBuilder.CreateIndex(
                name: "ix_external_book_search_jobs_query_hash_completed_at",
                table: "external_book_search_jobs",
                columns: new[] { "query_hash", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_external_book_search_jobs_status_next_retry_at",
                table: "external_book_search_jobs",
                columns: new[] { "status", "next_retry_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_book_search_jobs");
        }
    }
}
