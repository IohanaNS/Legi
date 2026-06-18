using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Legi.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "average_rating",
                table: "works",
                type: "numeric(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ratings_count",
                table: "works",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill each work's aggregate from its editions (weighted average,
            // matching the runtime recompute). Single-edition works get the book's
            // own values.
            migrationBuilder.Sql(
                """
                UPDATE works w SET
                    ratings_count = t.cnt,
                    average_rating = t.avg
                FROM (
                    SELECT work_id,
                           COALESCE(SUM(ratings_count), 0) AS cnt,
                           CASE WHEN COALESCE(SUM(ratings_count), 0) > 0
                                THEN ROUND(SUM(average_rating * ratings_count) / SUM(ratings_count), 2)
                                ELSE 0 END AS avg
                    FROM books
                    GROUP BY work_id
                ) t
                WHERE w.id = t.work_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "average_rating",
                table: "works");

            migrationBuilder.DropColumn(
                name: "ratings_count",
                table: "works");
        }
    }
}
