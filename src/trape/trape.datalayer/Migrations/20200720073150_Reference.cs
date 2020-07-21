using Microsoft.EntityFrameworkCore.Migrations;

namespace trape.datalayer.Migrations
{
    public partial class Reference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "order_id",
                table: "client_order",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "order_id",
                table: "client_order");
        }
    }
}
