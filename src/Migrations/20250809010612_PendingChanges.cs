using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tranzor.Migrations
{
    /// <inheritdoc />
    public partial class PendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_stop_cities_stop_id",
                table: "stop_cities",
                column: "stop_id");

            migrationBuilder.AddForeignKey(
                name: "fk_stop_cities_stops_stop_id",
                table: "stop_cities",
                column: "stop_id",
                principalTable: "gtfs_stops",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stop_cities_stops_stop_id",
                table: "stop_cities");

            migrationBuilder.DropIndex(
                name: "ix_stop_cities_stop_id",
                table: "stop_cities");
        }
    }
}
