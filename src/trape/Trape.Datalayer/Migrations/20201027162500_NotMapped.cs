using Microsoft.EntityFrameworkCore.Migrations;

namespace Trape.Datalayer.Migrations
{
    public partial class NotMapped : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "total_traded_quote_asset_volume",
                table: "ticks");

            migrationBuilder.RenameTable(
                name: "stats2m",
                schema: "stubs",
                newName: "stats2m");

            migrationBuilder.RenameTable(
                name: "stats2h",
                schema: "stubs",
                newName: "stats2h");

            migrationBuilder.RenameTable(
                name: "stats15s",
                schema: "stubs",
                newName: "stats15s");

            migrationBuilder.RenameTable(
                name: "stats10m",
                schema: "stubs",
                newName: "stats10m");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "stats2m",
                newName: "stats2m",
                newSchema: "stubs");

            migrationBuilder.RenameTable(
                name: "stats2h",
                newName: "stats2h",
                newSchema: "stubs");

            migrationBuilder.RenameTable(
                name: "stats15s",
                newName: "stats15s",
                newSchema: "stubs");

            migrationBuilder.RenameTable(
                name: "stats10m",
                newName: "stats10m",
                newSchema: "stubs");

            migrationBuilder.AddColumn<decimal>(
                name: "total_traded_quote_asset_volume",
                table: "ticks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
