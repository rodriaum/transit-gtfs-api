using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace TransitGtfsApi.Migrations
{
    /// <inheritdoc />
    public partial class AddGeographicColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<Point>(
                name: "location",
                table: "gtfs_stops",
                type: "geography (point)",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "geom",
                table: "gtfs_shapes",
                type: "geometry (point)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "location",
                table: "gtfs_stops");

            migrationBuilder.DropColumn(
                name: "geom",
                table: "gtfs_shapes");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
