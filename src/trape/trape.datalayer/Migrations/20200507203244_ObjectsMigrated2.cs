using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace trape.datalayer.Migrations
{
    public partial class ObjectsMigrated2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stats10m",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    r_symbol = table.Column<string>(nullable: true),
                    r_databasis = table.Column<int>(nullable: false),
                    r_slope_30m = table.Column<decimal>(nullable: false),
                    r_slope_1h = table.Column<decimal>(nullable: false),
                    r_slope_2h = table.Column<decimal>(nullable: false),
                    r_slope_3h = table.Column<decimal>(nullable: false),
                    r_movav_30m = table.Column<decimal>(nullable: false),
                    r_movav_1h = table.Column<decimal>(nullable: false),
                    r_movav_2h = table.Column<decimal>(nullable: false),
                    r_movav_3h = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats10m", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stats15s",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    r_symbol = table.Column<string>(nullable: true),
                    r_databasis = table.Column<int>(nullable: false),
                    r_slope_45s = table.Column<decimal>(nullable: false),
                    r_slope_1m = table.Column<decimal>(nullable: false),
                    r_slope_2m = table.Column<decimal>(nullable: false),
                    r_slope_3m = table.Column<decimal>(nullable: false),
                    r_movav_45s = table.Column<decimal>(nullable: false),
                    r_movav_1m = table.Column<decimal>(nullable: false),
                    r_movav_2m = table.Column<decimal>(nullable: false),
                    r_movav_3m = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats15s", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stats2h",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    r_symbol = table.Column<string>(nullable: true),
                    r_databasis = table.Column<int>(nullable: false),
                    r_slope_6h = table.Column<decimal>(nullable: false),
                    r_slope_12h = table.Column<decimal>(nullable: false),
                    r_slope_18h = table.Column<decimal>(nullable: false),
                    r_slope_1d = table.Column<decimal>(nullable: false),
                    r_movav_6h = table.Column<decimal>(nullable: false),
                    r_movav_12h = table.Column<decimal>(nullable: false),
                    r_movav_18h = table.Column<decimal>(nullable: false),
                    r_movav_24h = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats2h", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stats2m",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    r_symbol = table.Column<string>(nullable: true),
                    r_databasis = table.Column<int>(nullable: false),
                    r_slope_5m = table.Column<decimal>(nullable: false),
                    r_slope_7m = table.Column<decimal>(nullable: false),
                    r_slope_10m = table.Column<decimal>(nullable: false),
                    r_slope_15m = table.Column<decimal>(nullable: false),
                    r_movav_5m = table.Column<decimal>(nullable: false),
                    r_movav_7m = table.Column<decimal>(nullable: false),
                    r_movav_10m = table.Column<decimal>(nullable: false),
                    r_movav_15m = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats2m", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stats3s",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    r_symbol = table.Column<string>(nullable: true),
                    r_databasis = table.Column<int>(nullable: false),
                    r_slope_5s = table.Column<decimal>(nullable: false),
                    r_slope_10s = table.Column<decimal>(nullable: false),
                    r_slope_15s = table.Column<decimal>(nullable: false),
                    r_slope_30s = table.Column<decimal>(nullable: false),
                    r_movav_5s = table.Column<decimal>(nullable: false),
                    r_movav_10s = table.Column<decimal>(nullable: false),
                    r_movav_15s = table.Column<decimal>(nullable: false),
                    r_movav_30s = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats3s", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stats10m_id",
                table: "stats10m",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_stats15s_id",
                table: "stats15s",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_stats2h_id",
                table: "stats2h",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_stats2m_id",
                table: "stats2m",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_stats3s_id",
                table: "stats3s",
                column: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "stats3s");
        }
    }
}
