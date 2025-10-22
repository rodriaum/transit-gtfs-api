using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Tranzor.Migrations
{
    /// <inheritdoc />
    public partial class AddCitiesAndOtherTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    country_code = table.Column<string>(type: "text", nullable: false),
                    admin_level = table.Column<short>(type: "smallint", nullable: true),
                    source = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    geom = table.Column<MultiPolygon>(type: "geometry(MultiPolygon,4326)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stop_cities",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    city_id = table.Column<string>(type: "text", nullable: false),
                    stop_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stop_cities", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cities_geom",
                table: "cities",
                column: "geom")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropTable(
                name: "stop_cities");
        }
    }
}
