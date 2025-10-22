using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tranzor.Migrations
{
    /// <inheritdoc />
    public partial class CreateStopCity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stop_cities");
        }
    }
}
