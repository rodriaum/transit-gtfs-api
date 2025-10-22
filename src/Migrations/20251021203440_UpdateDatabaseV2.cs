using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tranzor.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabaseV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stop_cities_stops_stop_id",
                table: "stop_cities");

            migrationBuilder.DropTable(
                name: "gtfs_stop_times");

            migrationBuilder.DropIndex(
                name: "ix_stop_cities_stop_id",
                table: "stop_cities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gtfs_stop_times",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    arrival_time = table.Column<string>(type: "text", nullable: false),
                    departure_time = table.Column<string>(type: "text", nullable: false),
                    drop_off_type = table.Column<int>(type: "integer", nullable: true),
                    pickup_type = table.Column<int>(type: "integer", nullable: true),
                    shape_dist_traveled = table.Column<double>(type: "double precision", nullable: true),
                    stop_headsign = table.Column<string>(type: "text", nullable: true),
                    stop_id = table.Column<string>(type: "text", nullable: false),
                    stop_sequence = table.Column<int>(type: "integer", nullable: false),
                    timepoint = table.Column<int>(type: "integer", nullable: true),
                    trip_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_stop_times", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stop_cities_stop_id",
                table: "stop_cities",
                column: "stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_stop_times_stop_id",
                table: "gtfs_stop_times",
                column: "stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_stop_times_trip_id",
                table: "gtfs_stop_times",
                column: "trip_id");

            migrationBuilder.AddForeignKey(
                name: "fk_stop_cities_stops_stop_id",
                table: "stop_cities",
                column: "stop_id",
                principalTable: "gtfs_stops",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
