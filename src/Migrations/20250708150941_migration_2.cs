using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitGtfsApi.Migrations
{
    /// <inheritdoc />
    public partial class migration_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gtfs_attributions",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    agency_id = table.Column<string>(type: "text", nullable: true),
                    route_id = table.Column<string>(type: "text", nullable: true),
                    trip_id = table.Column<string>(type: "text", nullable: true),
                    organization_name = table.Column<string>(type: "text", nullable: false),
                    is_producer = table.Column<bool>(type: "boolean", nullable: false),
                    is_operator = table.Column<bool>(type: "boolean", nullable: false),
                    is_authority = table.Column<bool>(type: "boolean", nullable: false),
                    attribution_url = table.Column<string>(type: "text", nullable: true),
                    attribution_email = table.Column<string>(type: "text", nullable: true),
                    attribution_phone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_attributions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_fare_leg_rules",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    fare_leg_rule_id = table.Column<string>(type: "text", nullable: false),
                    fare_product_id = table.Column<string>(type: "text", nullable: true),
                    leg_group_id = table.Column<string>(type: "text", nullable: true),
                    network_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_fare_leg_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_fare_media",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    fare_media_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_fare_media", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_fare_products",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    fare_product_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    currency = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_fare_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_feed_info",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    feed_publisher_name = table.Column<string>(type: "text", nullable: false),
                    feed_publisher_url = table.Column<string>(type: "text", nullable: false),
                    feed_lang = table.Column<string>(type: "text", nullable: false),
                    feed_start_date = table.Column<string>(type: "text", nullable: true),
                    feed_end_date = table.Column<string>(type: "text", nullable: true),
                    feed_version = table.Column<string>(type: "text", nullable: true),
                    feed_contact_email = table.Column<string>(type: "text", nullable: true),
                    feed_contact_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_feed_info", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_networks",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    network_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_networks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_stop_areas",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    stop_area_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_stop_areas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_translations",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    table_name = table.Column<string>(type: "text", nullable: false),
                    field_name = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false),
                    translation_text = table.Column<string>(type: "text", nullable: false),
                    record_id = table.Column<string>(type: "text", nullable: true),
                    record_sub_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_translations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_attributions_organization_name",
                table: "gtfs_attributions",
                column: "organization_name");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_fare_leg_rules_fare_leg_rule_id",
                table: "gtfs_fare_leg_rules",
                column: "fare_leg_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_fare_media_fare_media_id",
                table: "gtfs_fare_media",
                column: "fare_media_id");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_fare_products_fare_product_id",
                table: "gtfs_fare_products",
                column: "fare_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_feed_info_feed_publisher_name",
                table: "gtfs_feed_info",
                column: "feed_publisher_name");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_networks_network_id",
                table: "gtfs_networks",
                column: "network_id");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_stop_areas_stop_area_id",
                table: "gtfs_stop_areas",
                column: "stop_area_id");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_translations_table_name",
                table: "gtfs_translations",
                column: "table_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gtfs_attributions");

            migrationBuilder.DropTable(
                name: "gtfs_fare_leg_rules");

            migrationBuilder.DropTable(
                name: "gtfs_fare_media");

            migrationBuilder.DropTable(
                name: "gtfs_fare_products");

            migrationBuilder.DropTable(
                name: "gtfs_feed_info");

            migrationBuilder.DropTable(
                name: "gtfs_networks");

            migrationBuilder.DropTable(
                name: "gtfs_stop_areas");

            migrationBuilder.DropTable(
                name: "gtfs_translations");
        }
    }
}
