using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitGtfsApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agencies",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    agency_id = table.Column<string>(type: "text", nullable: false),
                    agency_name = table.Column<string>(type: "text", nullable: false),
                    agency_url = table.Column<string>(type: "text", nullable: false),
                    agency_timezone = table.Column<string>(type: "text", nullable: false),
                    agency_lang = table.Column<string>(type: "text", nullable: false),
                    agency_phone = table.Column<string>(type: "text", nullable: true),
                    agency_fare_url = table.Column<string>(type: "text", nullable: true),
                    agency_email = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calendar_dates",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    service_id = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<string>(type: "text", nullable: false),
                    exception_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_dates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calendars",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    service_id = table.Column<string>(type: "text", nullable: false),
                    monday = table.Column<int>(type: "integer", nullable: false),
                    tuesday = table.Column<int>(type: "integer", nullable: false),
                    wednesday = table.Column<int>(type: "integer", nullable: false),
                    thursday = table.Column<int>(type: "integer", nullable: false),
                    friday = table.Column<int>(type: "integer", nullable: false),
                    saturday = table.Column<int>(type: "integer", nullable: false),
                    sunday = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<string>(type: "text", nullable: false),
                    end_date = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendars", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fare_attributes",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    fare_id = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: true),
                    currency_type = table.Column<string>(type: "text", nullable: false),
                    payment_method = table.Column<int>(type: "integer", nullable: false),
                    transfers = table.Column<int>(type: "integer", nullable: false),
                    transfer_duration = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fare_attributes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fare_rules",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    fare_id = table.Column<string>(type: "text", nullable: false),
                    route_id = table.Column<string>(type: "text", nullable: true),
                    origin_id = table.Column<string>(type: "text", nullable: true),
                    destination_id = table.Column<string>(type: "text", nullable: true),
                    contains_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fare_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    route_id = table.Column<string>(type: "text", nullable: false),
                    agency_id = table.Column<string>(type: "text", nullable: true),
                    route_short_name = table.Column<string>(type: "text", nullable: false),
                    route_long_name = table.Column<string>(type: "text", nullable: false),
                    route_desc = table.Column<string>(type: "text", nullable: true),
                    route_type = table.Column<int>(type: "integer", nullable: false),
                    route_url = table.Column<string>(type: "text", nullable: true),
                    route_color = table.Column<string>(type: "text", nullable: true),
                    route_text_color = table.Column<string>(type: "text", nullable: true),
                    route_sort_order = table.Column<int>(type: "integer", nullable: true),
                    continuous_pickup = table.Column<int>(type: "integer", nullable: true),
                    continuous_drop_off = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shapes",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    shape_id = table.Column<string>(type: "text", nullable: false),
                    shape_pt_lat = table.Column<double>(type: "double precision", nullable: false),
                    shape_pt_lon = table.Column<double>(type: "double precision", nullable: false),
                    shape_pt_sequence = table.Column<int>(type: "integer", nullable: false),
                    shape_dist_traveled = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shapes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stop_times",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    trip_id = table.Column<string>(type: "text", nullable: false),
                    arrival_time = table.Column<string>(type: "text", nullable: false),
                    departure_time = table.Column<string>(type: "text", nullable: false),
                    stop_id = table.Column<string>(type: "text", nullable: false),
                    stop_sequence = table.Column<int>(type: "integer", nullable: false),
                    stop_headsign = table.Column<string>(type: "text", nullable: true),
                    pickup_type = table.Column<int>(type: "integer", nullable: true),
                    drop_off_type = table.Column<int>(type: "integer", nullable: true),
                    shape_dist_traveled = table.Column<double>(type: "double precision", nullable: true),
                    timepoint = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stop_times", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stops",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    stop_id = table.Column<string>(type: "text", nullable: false),
                    stop_code = table.Column<string>(type: "text", nullable: true),
                    stop_name = table.Column<string>(type: "text", nullable: false),
                    stop_desc = table.Column<string>(type: "text", nullable: true),
                    stop_lat = table.Column<double>(type: "double precision", nullable: false),
                    stop_lon = table.Column<double>(type: "double precision", nullable: false),
                    zone_id = table.Column<string>(type: "text", nullable: false),
                    stop_url = table.Column<string>(type: "text", nullable: false),
                    location_type = table.Column<int>(type: "integer", nullable: true),
                    parent_station = table.Column<string>(type: "text", nullable: true),
                    stop_timezone = table.Column<string>(type: "text", nullable: true),
                    wheelchair_boarding = table.Column<int>(type: "integer", nullable: true),
                    platform_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stops", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transfers",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    from_stop_id = table.Column<string>(type: "text", nullable: false),
                    to_stop_id = table.Column<string>(type: "text", nullable: false),
                    transfer_type = table.Column<int>(type: "integer", nullable: false),
                    min_transfer_time = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transfers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    route_id = table.Column<string>(type: "text", nullable: false),
                    service_id = table.Column<string>(type: "text", nullable: false),
                    trip_id = table.Column<string>(type: "text", nullable: false),
                    trip_headsign = table.Column<string>(type: "text", nullable: true),
                    trip_short_name = table.Column<string>(type: "text", nullable: true),
                    direction_id = table.Column<int>(type: "integer", nullable: true),
                    block_id = table.Column<string>(type: "text", nullable: true),
                    shape_id = table.Column<string>(type: "text", nullable: true),
                    wheelchair_accessible = table.Column<int>(type: "integer", nullable: true),
                    bikes_allowed = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trips", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agencies_agency_id",
                table: "agencies",
                column: "agency_id");

            migrationBuilder.CreateIndex(
                name: "ix_calendar_dates_service_id",
                table: "calendar_dates",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_calendars_service_id",
                table: "calendars",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_fare_attributes_fare_id",
                table: "fare_attributes",
                column: "fare_id");

            migrationBuilder.CreateIndex(
                name: "ix_fare_rules_fare_id",
                table: "fare_rules",
                column: "fare_id");

            migrationBuilder.CreateIndex(
                name: "ix_routes_route_id",
                table: "routes",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ix_shapes_shape_id",
                table: "shapes",
                column: "shape_id");

            migrationBuilder.CreateIndex(
                name: "ix_stop_times_stop_id",
                table: "stop_times",
                column: "stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_stop_times_trip_id",
                table: "stop_times",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_stops_stop_id",
                table: "stops",
                column: "stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfers_from_stop_id",
                table: "transfers",
                column: "from_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfers_to_stop_id",
                table: "transfers",
                column: "to_stop_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_route_id",
                table: "trips",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_service_id",
                table: "trips",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_trips_trip_id",
                table: "trips",
                column: "trip_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agencies");

            migrationBuilder.DropTable(
                name: "calendar_dates");

            migrationBuilder.DropTable(
                name: "calendars");

            migrationBuilder.DropTable(
                name: "fare_attributes");

            migrationBuilder.DropTable(
                name: "fare_rules");

            migrationBuilder.DropTable(
                name: "routes");

            migrationBuilder.DropTable(
                name: "shapes");

            migrationBuilder.DropTable(
                name: "stop_times");

            migrationBuilder.DropTable(
                name: "stops");

            migrationBuilder.DropTable(
                name: "transfers");

            migrationBuilder.DropTable(
                name: "trips");
        }
    }
}
