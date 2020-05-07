using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace trape.datalayer.Migrations
{
    public partial class ObjectsMigrated : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_balance_account_info_account_info_id",
                table: "balance");

            migrationBuilder.DropForeignKey(
                name: "FK_order_order_list_order_list_id",
                table: "order");

            migrationBuilder.DropForeignKey(
                name: "FK_order_update_order_order_id",
                table: "order_update");

            migrationBuilder.DropForeignKey(
                name: "FK_order_update_order_list_order_list_id",
                table: "order_update");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tick",
                table: "tick");

            migrationBuilder.DropPrimaryKey(
                name: "PK_symbol",
                table: "symbol");

            migrationBuilder.DropPrimaryKey(
                name: "PK_order_update",
                table: "order_update");

            migrationBuilder.DropPrimaryKey(
                name: "PK_order_list",
                table: "order_list");

            migrationBuilder.DropPrimaryKey(
                name: "PK_order",
                table: "order");

            migrationBuilder.DropIndex(
                name: "IX_order_client_order_id",
                table: "order");

            migrationBuilder.DropPrimaryKey(
                name: "PK_kline",
                table: "kline");

            migrationBuilder.DropPrimaryKey(
                name: "PK_book_tick",
                table: "book_tick");

            migrationBuilder.DropPrimaryKey(
                name: "PK_balance_update",
                table: "balance_update");

            migrationBuilder.DropPrimaryKey(
                name: "PK_balance",
                table: "balance");

            migrationBuilder.DropPrimaryKey(
                name: "PK_account_info",
                table: "account_info");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "order_update");

            migrationBuilder.RenameTable(
                name: "tick",
                newName: "ticks");

            migrationBuilder.RenameTable(
                name: "symbol",
                newName: "symbols");

            migrationBuilder.RenameTable(
                name: "order_update",
                newName: "order_updates");

            migrationBuilder.RenameTable(
                name: "order_list",
                newName: "order_lists");

            migrationBuilder.RenameTable(
                name: "order",
                newName: "orders");

            migrationBuilder.RenameTable(
                name: "kline",
                newName: "klines");

            migrationBuilder.RenameTable(
                name: "book_tick",
                newName: "book_ticks");

            migrationBuilder.RenameTable(
                name: "balance_update",
                newName: "balance_updates");

            migrationBuilder.RenameTable(
                name: "balance",
                newName: "balances");

            migrationBuilder.RenameTable(
                name: "account_info",
                newName: "account_infos");

            migrationBuilder.RenameIndex(
                name: "IX_tick_statistics_open_time_statistics_close_time",
                table: "ticks",
                newName: "IX_ticks_statistics_open_time_statistics_close_time");

            migrationBuilder.RenameIndex(
                name: "IX_tick_id",
                table: "ticks",
                newName: "IX_ticks_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_update_symbol_side",
                table: "order_updates",
                newName: "IX_order_updates_symbol_side");

            migrationBuilder.RenameIndex(
                name: "IX_order_update_original_client_order_id",
                table: "order_updates",
                newName: "IX_order_updates_original_client_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_update_order_list_id",
                table: "order_updates",
                newName: "IX_order_updates_order_list_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_update_order_id",
                table: "order_updates",
                newName: "IX_order_updates_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_update_id",
                table: "order_updates",
                newName: "IX_order_updates_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_update_client_order_id",
                table: "order_updates",
                newName: "IX_order_updates_client_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_list_symbol",
                table: "order_lists",
                newName: "IX_order_lists_symbol");

            migrationBuilder.RenameIndex(
                name: "IX_order_list_order_list_id",
                table: "order_lists",
                newName: "IX_order_lists_order_list_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_order_list_id",
                table: "orders",
                newName: "IX_orders_order_list_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_order_id",
                table: "orders",
                newName: "IX_orders_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_id",
                table: "orders",
                newName: "IX_orders_id");

            migrationBuilder.RenameIndex(
                name: "IX_kline_open_time_interval_symbol",
                table: "klines",
                newName: "IX_klines_open_time_interval_symbol");

            migrationBuilder.RenameIndex(
                name: "IX_kline_id",
                table: "klines",
                newName: "IX_klines_id");

            migrationBuilder.RenameIndex(
                name: "IX_book_tick_created_on_symbol",
                table: "book_ticks",
                newName: "IX_book_ticks_created_on_symbol");

            migrationBuilder.RenameIndex(
                name: "IX_book_tick_update_id",
                table: "book_ticks",
                newName: "IX_book_ticks_update_id");

            migrationBuilder.RenameIndex(
                name: "IX_balance_update_id",
                table: "balance_updates",
                newName: "IX_balance_updates_id");

            migrationBuilder.RenameIndex(
                name: "IX_balance_id",
                table: "balances",
                newName: "IX_balances_id");

            migrationBuilder.RenameIndex(
                name: "IX_balance_account_info_id",
                table: "balances",
                newName: "IX_balances_account_info_id");

            migrationBuilder.RenameIndex(
                name: "IX_account_info_id",
                table: "account_infos",
                newName: "IX_account_infos_id");

            migrationBuilder.AddColumn<decimal>(
                name: "total",
                table: "balances",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ticks",
                table: "ticks",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_symbols",
                table: "symbols",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_order_updates",
                table: "order_updates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_order_lists",
                table: "order_lists",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_orders",
                table: "orders",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_klines",
                table: "klines",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_book_ticks",
                table: "book_ticks",
                column: "update_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_balance_updates",
                table: "balance_updates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_balances",
                table: "balances",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_account_infos",
                table: "account_infos",
                column: "id");

            migrationBuilder.CreateTable(
                name: "client_order",
                columns: table => new
                {
                    id = table.Column<string>(nullable: false),
                    created_on = table.Column<DateTime>(nullable: false),
                    symbol = table.Column<string>(nullable: true),
                    side = table.Column<int>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    quote_order_quantity = table.Column<decimal>(nullable: false),
                    price = table.Column<decimal>(nullable: false),
                    order_response_type = table.Column<int>(nullable: false),
                    time_in_force = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_order", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "placed_orders",
                columns: table => new
                {
                    order_id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    margin_buy_borrow_asset = table.Column<string>(nullable: true),
                    margin_buy_borrow_amount = table.Column<decimal>(nullable: true),
                    stop_price = table.Column<decimal>(nullable: true),
                    side = table.Column<int>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    time_in_force = table.Column<int>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    original_quote_order_quantity = table.Column<decimal>(nullable: false),
                    cummulative_quote_quantity = table.Column<decimal>(nullable: false),
                    executed_quantity = table.Column<decimal>(nullable: false),
                    original_quantity = table.Column<decimal>(nullable: false),
                    price = table.Column<decimal>(nullable: false),
                    transaction_time = table.Column<DateTime>(nullable: false),
                    original_client_order_id = table.Column<string>(nullable: true),
                    client_order_id = table.Column<string>(nullable: true),
                    symbol = table.Column<string>(nullable: true),
                    order_list_id = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_placed_orders", x => x.order_id);
                });

            migrationBuilder.CreateTable(
                name: "recommendations",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    symbol = table.Column<string>(nullable: true),
                    created_on = table.Column<DateTime>(nullable: false),
                    action = table.Column<int>(nullable: false),
                    price = table.Column<decimal>(nullable: false),
                    slope5s = table.Column<decimal>(nullable: false),
                    moving_average5s = table.Column<decimal>(nullable: false),
                    slope10s = table.Column<decimal>(nullable: false),
                    moving_average10s = table.Column<decimal>(nullable: false),
                    slope15s = table.Column<decimal>(nullable: false),
                    moving_average15s = table.Column<decimal>(nullable: false),
                    slope30s = table.Column<decimal>(nullable: false),
                    moving_average30s = table.Column<decimal>(nullable: false),
                    slope45s = table.Column<decimal>(nullable: false),
                    moving_average45s = table.Column<decimal>(nullable: false),
                    slope1m = table.Column<decimal>(nullable: false),
                    moving_average1m = table.Column<decimal>(nullable: false),
                    slope2m = table.Column<decimal>(nullable: false),
                    moving_average2m = table.Column<decimal>(nullable: false),
                    slope3m = table.Column<decimal>(nullable: false),
                    moving_average3m = table.Column<decimal>(nullable: false),
                    slope5m = table.Column<decimal>(nullable: false),
                    moving_average5m = table.Column<decimal>(nullable: false),
                    slope7m = table.Column<decimal>(nullable: false),
                    moving_average7m = table.Column<decimal>(nullable: false),
                    slope10m = table.Column<decimal>(nullable: false),
                    moving_average10m = table.Column<decimal>(nullable: false),
                    slope15m = table.Column<decimal>(nullable: false),
                    moving_average15m = table.Column<decimal>(nullable: false),
                    slope30m = table.Column<decimal>(nullable: false),
                    moving_average30m = table.Column<decimal>(nullable: false),
                    slope1h = table.Column<decimal>(nullable: false),
                    moving_average1h = table.Column<decimal>(nullable: false),
                    slope2h = table.Column<decimal>(nullable: false),
                    moving_average2h = table.Column<decimal>(nullable: false),
                    slope3h = table.Column<decimal>(nullable: false),
                    moving_average3h = table.Column<decimal>(nullable: false),
                    slope6h = table.Column<decimal>(nullable: false),
                    moving_average6h = table.Column<decimal>(nullable: false),
                    slope12h = table.Column<decimal>(nullable: false),
                    moving_average12h = table.Column<decimal>(nullable: false),
                    slope18h = table.Column<decimal>(nullable: false),
                    moving_average18h = table.Column<decimal>(nullable: false),
                    slope1d = table.Column<decimal>(nullable: false),
                    moving_average1d = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommendations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_trades",
                columns: table => new
                {
                    trade_id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    price = table.Column<decimal>(nullable: false),
                    quantity = table.Column<decimal>(nullable: false),
                    commission = table.Column<decimal>(nullable: false),
                    commission_asset = table.Column<string>(nullable: true),
                    consumed_quantity = table.Column<decimal>(nullable: false),
                    placed_order_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_trades", x => x.trade_id);
                    table.ForeignKey(
                        name: "FK_order_trades_placed_orders_placed_order_id",
                        column: x => x.placed_order_id,
                        principalTable: "placed_orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orders_client_order_id",
                table: "orders",
                column: "client_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_order_created_on",
                table: "client_order",
                column: "created_on");

            migrationBuilder.CreateIndex(
                name: "IX_client_order_id",
                table: "client_order",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_client_order_symbol",
                table: "client_order",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "IX_order_trades_placed_order_id",
                table: "order_trades",
                column: "placed_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_trades_trade_id",
                table: "order_trades",
                column: "trade_id");

            migrationBuilder.CreateIndex(
                name: "IX_placed_orders_client_order_id",
                table: "placed_orders",
                column: "client_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_placed_orders_order_id",
                table: "placed_orders",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_placed_orders_order_list_id",
                table: "placed_orders",
                column: "order_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_placed_orders_original_client_order_id",
                table: "placed_orders",
                column: "original_client_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_placed_orders_transaction_time_symbol",
                table: "placed_orders",
                columns: new[] { "transaction_time", "symbol" });

            migrationBuilder.CreateIndex(
                name: "IX_recommendations_id",
                table: "recommendations",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_recommendations_created_on_symbol",
                table: "recommendations",
                columns: new[] { "created_on", "symbol" });

            migrationBuilder.AddForeignKey(
                name: "FK_balances_account_infos_account_info_id",
                table: "balances",
                column: "account_info_id",
                principalTable: "account_infos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_order_updates_orders_order_id",
                table: "order_updates",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_order_updates_order_lists_order_list_id",
                table: "order_updates",
                column: "order_list_id",
                principalTable: "order_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_client_order_client_order_id",
                table: "orders",
                column: "client_order_id",
                principalTable: "client_order",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_order_lists_order_list_id",
                table: "orders",
                column: "order_list_id",
                principalTable: "order_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_balances_account_infos_account_info_id",
                table: "balances");

            migrationBuilder.DropForeignKey(
                name: "FK_order_updates_orders_order_id",
                table: "order_updates");

            migrationBuilder.DropForeignKey(
                name: "FK_order_updates_order_lists_order_list_id",
                table: "order_updates");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_client_order_client_order_id",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_order_lists_order_list_id",
                table: "orders");

            migrationBuilder.DropTable(
                name: "client_order");

            migrationBuilder.DropTable(
                name: "order_trades");

            migrationBuilder.DropTable(
                name: "recommendations");

            migrationBuilder.DropTable(
                name: "placed_orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ticks",
                table: "ticks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_symbols",
                table: "symbols");

            migrationBuilder.DropPrimaryKey(
                name: "PK_orders",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_client_order_id",
                table: "orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_order_updates",
                table: "order_updates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_order_lists",
                table: "order_lists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_klines",
                table: "klines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_book_ticks",
                table: "book_ticks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_balances",
                table: "balances");

            migrationBuilder.DropPrimaryKey(
                name: "PK_balance_updates",
                table: "balance_updates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_account_infos",
                table: "account_infos");

            migrationBuilder.DropColumn(
                name: "total",
                table: "balances");

            migrationBuilder.RenameTable(
                name: "ticks",
                newName: "tick");

            migrationBuilder.RenameTable(
                name: "symbols",
                newName: "symbol");

            migrationBuilder.RenameTable(
                name: "orders",
                newName: "order");

            migrationBuilder.RenameTable(
                name: "order_updates",
                newName: "order_update");

            migrationBuilder.RenameTable(
                name: "order_lists",
                newName: "order_list");

            migrationBuilder.RenameTable(
                name: "klines",
                newName: "kline");

            migrationBuilder.RenameTable(
                name: "book_ticks",
                newName: "book_tick");

            migrationBuilder.RenameTable(
                name: "balances",
                newName: "balance");

            migrationBuilder.RenameTable(
                name: "balance_updates",
                newName: "balance_update");

            migrationBuilder.RenameTable(
                name: "account_infos",
                newName: "account_info");

            migrationBuilder.RenameIndex(
                name: "IX_ticks_statistics_open_time_statistics_close_time",
                table: "tick",
                newName: "IX_tick_statistics_open_time_statistics_close_time");

            migrationBuilder.RenameIndex(
                name: "IX_ticks_id",
                table: "tick",
                newName: "IX_tick_id");

            migrationBuilder.RenameIndex(
                name: "IX_orders_order_list_id",
                table: "order",
                newName: "IX_order_order_list_id");

            migrationBuilder.RenameIndex(
                name: "IX_orders_order_id",
                table: "order",
                newName: "IX_order_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_orders_id",
                table: "order",
                newName: "IX_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_updates_symbol_side",
                table: "order_update",
                newName: "IX_order_update_symbol_side");

            migrationBuilder.RenameIndex(
                name: "IX_order_updates_original_client_order_id",
                table: "order_update",
                newName: "IX_order_update_original_client_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_updates_order_list_id",
                table: "order_update",
                newName: "IX_order_update_order_list_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_updates_order_id",
                table: "order_update",
                newName: "IX_order_update_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_updates_id",
                table: "order_update",
                newName: "IX_order_update_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_updates_client_order_id",
                table: "order_update",
                newName: "IX_order_update_client_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_order_lists_symbol",
                table: "order_list",
                newName: "IX_order_list_symbol");

            migrationBuilder.RenameIndex(
                name: "IX_order_lists_order_list_id",
                table: "order_list",
                newName: "IX_order_list_order_list_id");

            migrationBuilder.RenameIndex(
                name: "IX_klines_open_time_interval_symbol",
                table: "kline",
                newName: "IX_kline_open_time_interval_symbol");

            migrationBuilder.RenameIndex(
                name: "IX_klines_id",
                table: "kline",
                newName: "IX_kline_id");

            migrationBuilder.RenameIndex(
                name: "IX_book_ticks_created_on_symbol",
                table: "book_tick",
                newName: "IX_book_tick_created_on_symbol");

            migrationBuilder.RenameIndex(
                name: "IX_book_ticks_update_id",
                table: "book_tick",
                newName: "IX_book_tick_update_id");

            migrationBuilder.RenameIndex(
                name: "IX_balances_id",
                table: "balance",
                newName: "IX_balance_id");

            migrationBuilder.RenameIndex(
                name: "IX_balances_account_info_id",
                table: "balance",
                newName: "IX_balance_account_info_id");

            migrationBuilder.RenameIndex(
                name: "IX_balance_updates_id",
                table: "balance_update",
                newName: "IX_balance_update_id");

            migrationBuilder.RenameIndex(
                name: "IX_account_infos_id",
                table: "account_info",
                newName: "IX_account_info_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_on",
                table: "order_update",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_tick",
                table: "tick",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_symbol",
                table: "symbol",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_order",
                table: "order",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_order_update",
                table: "order_update",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_order_list",
                table: "order_list",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_kline",
                table: "kline",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_book_tick",
                table: "book_tick",
                column: "update_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_balance",
                table: "balance",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_balance_update",
                table: "balance_update",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_account_info",
                table: "account_info",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_order_client_order_id",
                table: "order",
                column: "client_order_id");

            migrationBuilder.AddForeignKey(
                name: "FK_balance_account_info_account_info_id",
                table: "balance",
                column: "account_info_id",
                principalTable: "account_info",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_order_order_list_order_list_id",
                table: "order",
                column: "order_list_id",
                principalTable: "order_list",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_order_update_order_order_id",
                table: "order_update",
                column: "order_id",
                principalTable: "order",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_order_update_order_list_order_list_id",
                table: "order_update",
                column: "order_list_id",
                principalTable: "order_list",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
