using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitGtfsApi.Migrations
{
    /// <inheritdoc />
    public partial class migration_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_trips",
                table: "trips");

            migrationBuilder.DropPrimaryKey(
                name: "pk_transfers",
                table: "transfers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_stops",
                table: "stops");

            migrationBuilder.DropPrimaryKey(
                name: "pk_stop_times",
                table: "stop_times");

            migrationBuilder.DropPrimaryKey(
                name: "pk_shapes",
                table: "shapes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_routes",
                table: "routes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fare_rules",
                table: "fare_rules");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fare_attributes",
                table: "fare_attributes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_calendars",
                table: "calendars");

            migrationBuilder.DropPrimaryKey(
                name: "pk_calendar_dates",
                table: "calendar_dates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_agencies",
                table: "agencies");

            migrationBuilder.RenameTable(
                name: "trips",
                newName: "gtfs_trips");

            migrationBuilder.RenameTable(
                name: "transfers",
                newName: "gtfs_transfers");

            migrationBuilder.RenameTable(
                name: "stops",
                newName: "gtfs_stops");

            migrationBuilder.RenameTable(
                name: "stop_times",
                newName: "gtfs_stop_times");

            migrationBuilder.RenameTable(
                name: "shapes",
                newName: "gtfs_shapes");

            migrationBuilder.RenameTable(
                name: "routes",
                newName: "gtfs_routes");

            migrationBuilder.RenameTable(
                name: "fare_rules",
                newName: "gtfs_fare_rules");

            migrationBuilder.RenameTable(
                name: "fare_attributes",
                newName: "gtfs_fare_attributes");

            migrationBuilder.RenameTable(
                name: "calendars",
                newName: "gtfs_calendars");

            migrationBuilder.RenameTable(
                name: "calendar_dates",
                newName: "gtfs_calendar_dates");

            migrationBuilder.RenameTable(
                name: "agencies",
                newName: "gtfs_agencies");

            migrationBuilder.RenameIndex(
                name: "ix_trips_trip_id",
                table: "gtfs_trips",
                newName: "ix_gtfs_trips_trip_id");

            migrationBuilder.RenameIndex(
                name: "ix_trips_service_id",
                table: "gtfs_trips",
                newName: "ix_gtfs_trips_service_id");

            migrationBuilder.RenameIndex(
                name: "ix_trips_route_id",
                table: "gtfs_trips",
                newName: "ix_gtfs_trips_route_id");

            migrationBuilder.RenameIndex(
                name: "ix_transfers_to_stop_id",
                table: "gtfs_transfers",
                newName: "ix_gtfs_transfers_to_stop_id");

            migrationBuilder.RenameIndex(
                name: "ix_transfers_from_stop_id",
                table: "gtfs_transfers",
                newName: "ix_gtfs_transfers_from_stop_id");

            migrationBuilder.RenameIndex(
                name: "ix_stops_stop_id",
                table: "gtfs_stops",
                newName: "ix_gtfs_stops_stop_id");

            migrationBuilder.RenameIndex(
                name: "ix_stop_times_trip_id",
                table: "gtfs_stop_times",
                newName: "ix_gtfs_stop_times_trip_id");

            migrationBuilder.RenameIndex(
                name: "ix_stop_times_stop_id",
                table: "gtfs_stop_times",
                newName: "ix_gtfs_stop_times_stop_id");

            migrationBuilder.RenameIndex(
                name: "ix_shapes_shape_id",
                table: "gtfs_shapes",
                newName: "ix_gtfs_shapes_shape_id");

            migrationBuilder.RenameIndex(
                name: "ix_routes_route_id",
                table: "gtfs_routes",
                newName: "ix_gtfs_routes_route_id");

            migrationBuilder.RenameIndex(
                name: "ix_fare_rules_fare_id",
                table: "gtfs_fare_rules",
                newName: "ix_gtfs_fare_rules_fare_id");

            migrationBuilder.RenameIndex(
                name: "ix_fare_attributes_fare_id",
                table: "gtfs_fare_attributes",
                newName: "ix_gtfs_fare_attributes_fare_id");

            migrationBuilder.RenameIndex(
                name: "ix_calendars_service_id",
                table: "gtfs_calendars",
                newName: "ix_gtfs_calendars_service_id");

            migrationBuilder.RenameIndex(
                name: "ix_calendar_dates_service_id",
                table: "gtfs_calendar_dates",
                newName: "ix_gtfs_calendar_dates_service_id");

            migrationBuilder.RenameIndex(
                name: "ix_agencies_agency_id",
                table: "gtfs_agencies",
                newName: "ix_gtfs_agencies_agency_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gtfs_trips",
                table: "gtfs_trips",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gtfs_transfers",
                table: "gtfs_transfers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gtfs_stops",
                table: "gtfs_stops",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gtfs_stop_times",
                table: "gtfs_stop_times",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gtfs_shapes",
                table: "gtfs_shapes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gtfs_routes",
                table: "gtfs_routes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gtfs_fare_rules",
                table: "gtfs_fare_rules",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gtfs_fare_attributes",
                table: "gtfs_fare_attributes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gtfs_calendars",
                table: "gtfs_calendars",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gtfs_calendar_dates",
                table: "gtfs_calendar_dates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_gtfs_agencies",
                table: "gtfs_agencies",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_gtfs_trips",
                table: "gtfs_trips");

            migrationBuilder.DropPrimaryKey(
                name: "pk_gtfs_transfers",
                table: "gtfs_transfers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_gtfs_stops",
                table: "gtfs_stops");

            migrationBuilder.DropPrimaryKey(
                name: "pk_gtfs_stop_times",
                table: "gtfs_stop_times");

            migrationBuilder.DropPrimaryKey(
                name: "pk_gtfs_shapes",
                table: "gtfs_shapes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_gtfs_routes",
                table: "gtfs_routes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_gtfs_fare_rules",
                table: "gtfs_fare_rules");

            migrationBuilder.DropPrimaryKey(
                name: "pk_gtfs_fare_attributes",
                table: "gtfs_fare_attributes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_gtfs_calendars",
                table: "gtfs_calendars");

            migrationBuilder.DropPrimaryKey(
                name: "pk_gtfs_calendar_dates",
                table: "gtfs_calendar_dates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_gtfs_agencies",
                table: "gtfs_agencies");

            migrationBuilder.RenameTable(
                name: "gtfs_trips",
                newName: "trips");

            migrationBuilder.RenameTable(
                name: "gtfs_transfers",
                newName: "transfers");

            migrationBuilder.RenameTable(
                name: "gtfs_stops",
                newName: "stops");

            migrationBuilder.RenameTable(
                name: "gtfs_stop_times",
                newName: "stop_times");

            migrationBuilder.RenameTable(
                name: "gtfs_shapes",
                newName: "shapes");

            migrationBuilder.RenameTable(
                name: "gtfs_routes",
                newName: "routes");

            migrationBuilder.RenameTable(
                name: "gtfs_fare_rules",
                newName: "fare_rules");

            migrationBuilder.RenameTable(
                name: "gtfs_fare_attributes",
                newName: "fare_attributes");

            migrationBuilder.RenameTable(
                name: "gtfs_calendars",
                newName: "calendars");

            migrationBuilder.RenameTable(
                name: "gtfs_calendar_dates",
                newName: "calendar_dates");

            migrationBuilder.RenameTable(
                name: "gtfs_agencies",
                newName: "agencies");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_trips_trip_id",
                table: "trips",
                newName: "ix_trips_trip_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_trips_service_id",
                table: "trips",
                newName: "ix_trips_service_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_trips_route_id",
                table: "trips",
                newName: "ix_trips_route_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_transfers_to_stop_id",
                table: "transfers",
                newName: "ix_transfers_to_stop_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_transfers_from_stop_id",
                table: "transfers",
                newName: "ix_transfers_from_stop_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_stops_stop_id",
                table: "stops",
                newName: "ix_stops_stop_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_stop_times_trip_id",
                table: "stop_times",
                newName: "ix_stop_times_trip_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_stop_times_stop_id",
                table: "stop_times",
                newName: "ix_stop_times_stop_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_shapes_shape_id",
                table: "shapes",
                newName: "ix_shapes_shape_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_routes_route_id",
                table: "routes",
                newName: "ix_routes_route_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_fare_rules_fare_id",
                table: "fare_rules",
                newName: "ix_fare_rules_fare_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_fare_attributes_fare_id",
                table: "fare_attributes",
                newName: "ix_fare_attributes_fare_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_calendars_service_id",
                table: "calendars",
                newName: "ix_calendars_service_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_calendar_dates_service_id",
                table: "calendar_dates",
                newName: "ix_calendar_dates_service_id");

            migrationBuilder.RenameIndex(
                name: "ix_gtfs_agencies_agency_id",
                table: "agencies",
                newName: "ix_agencies_agency_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_trips",
                table: "trips",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_transfers",
                table: "transfers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_stops",
                table: "stops",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_stop_times",
                table: "stop_times",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_shapes",
                table: "shapes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_routes",
                table: "routes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fare_rules",
                table: "fare_rules",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fare_attributes",
                table: "fare_attributes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_calendars",
                table: "calendars",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_calendar_dates",
                table: "calendar_dates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_agencies",
                table: "agencies",
                column: "id");
        }
    }
}
