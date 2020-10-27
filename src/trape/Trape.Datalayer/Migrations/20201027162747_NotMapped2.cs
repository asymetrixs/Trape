using Microsoft.EntityFrameworkCore.Migrations;

namespace Trape.Datalayer.Migrations
{
    public partial class NotMapped2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stats10m");

            migrationBuilder.DropTable(
                name: "stats15s");

            migrationBuilder.DropTable(
                name: "stats2h");

            migrationBuilder.DropTable(
                name: "stats2m");

            migrationBuilder.DropTable(
                name: "stats3s",
                schema: "stubs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stats10m",
                columns: table => new
                {
                    r_databasis = table.Column<int>(type: "integer", nullable: false),
                    r_movav_1h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_2h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_30m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_3h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_1h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_2h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_30m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_3h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_symbol = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "stats15s",
                columns: table => new
                {
                    r_databasis = table.Column<int>(type: "integer", nullable: false),
                    r_movav_1m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_2m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_3m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_45s = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_1m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_2m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_3m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_45s = table.Column<decimal>(type: "numeric", nullable: false),
                    r_symbol = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "stats2h",
                columns: table => new
                {
                    r_databasis = table.Column<int>(type: "integer", nullable: false),
                    r_movav_12h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_18h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_1d = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_6h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_12h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_18h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_1d = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_6h = table.Column<decimal>(type: "numeric", nullable: false),
                    r_symbol = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "stats2m",
                columns: table => new
                {
                    r_databasis = table.Column<int>(type: "integer", nullable: false),
                    r_movav_10m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_15m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_5m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_7m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_10m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_15m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_5m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_7m = table.Column<decimal>(type: "numeric", nullable: false),
                    r_symbol = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "stats3s",
                schema: "stubs",
                columns: table => new
                {
                    r_databasis = table.Column<int>(type: "integer", nullable: false),
                    r_movav_10s = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_15s = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_30s = table.Column<decimal>(type: "numeric", nullable: false),
                    r_movav_5s = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_10s = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_15s = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_30s = table.Column<decimal>(type: "numeric", nullable: false),
                    r_slope_5s = table.Column<decimal>(type: "numeric", nullable: false),
                    r_symbol = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });
        }
    }
}
