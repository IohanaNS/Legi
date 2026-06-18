using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverIngestionJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cover_ingestion_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    isbn = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    no_cover_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    transient_failures = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cover_ingestion_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cover_ingestion_jobs_active_book_id",
                table: "cover_ingestion_jobs",
                column: "book_id",
                unique: true,
                filter: "\"status\" IN ('Pending', 'Processing')");

            migrationBuilder.CreateIndex(
                name: "ix_cover_ingestion_jobs_status_next_retry_at",
                table: "cover_ingestion_jobs",
                columns: new[] { "status", "next_retry_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cover_ingestion_jobs");
        }
    }
}
