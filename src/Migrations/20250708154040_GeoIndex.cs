using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitGtfsApi.Migrations
{
    /// <inheritdoc />
    public partial class GeoIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_gtfs_stops_location",
                table: "gtfs_stops",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_shapes_geom",
                table: "gtfs_shapes",
                column: "geom")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_gtfs_stops_location",
                table: "gtfs_stops");

            migrationBuilder.DropIndex(
                name: "ix_gtfs_shapes_geom",
                table: "gtfs_shapes");
        }
    }
}
