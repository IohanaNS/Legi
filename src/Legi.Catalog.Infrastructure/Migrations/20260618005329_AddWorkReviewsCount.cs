using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkReviewsCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reviews_count",
                table: "works",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill each work's reviews count from the sum across its editions.
            migrationBuilder.Sql(
                """
                UPDATE works w SET reviews_count = t.cnt
                FROM (SELECT work_id, COALESCE(SUM(reviews_count), 0) AS cnt FROM books GROUP BY work_id) t
                WHERE w.id = t.work_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reviews_count",
                table: "works");
        }
    }
}
