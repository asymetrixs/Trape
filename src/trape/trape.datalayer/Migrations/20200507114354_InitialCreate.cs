using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace trape.datalayer.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_info",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    updated_on = table.Column<DateTime>(nullable: false),
                    maker_commission = table.Column<decimal>(nullable: false),
                    taker_commission = table.Column<decimal>(nullable: false),
                    buyer_commission = table.Column<decimal>(nullable: false),
                    seller_commission = table.Column<decimal>(nullable: false),
                    can_trade = table.Column<bool>(nullable: false),
                    can_withdraw = table.Column<bool>(nullable: false),
                    can_deposit = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_info", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "balance_update",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asset = table.Column<string>(nullable: true),
                    balance_delta = table.Column<decimal>(nullable: false),
                    clear_time = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_balance_update", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "book_tick",
                columns: table => new
                {
                    update_id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    symbol = table.Column<string>(nullable: true),
                    best_bid_price = table.Column<decimal>(nullable: false),
                    best_bid_quantity = table.Column<decimal>(nullable: false),
                    best_ask_price = table.Column<decimal>(nullable: false),
                    best_ask_quantity = table.Column<decimal>(nullable: false),
                    created_on = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_tick", x => x.update_id);
                });

            migrationBuilder.CreateTable(
                name: "kline",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    open = table.Column<decimal>(nullable: false),
                    quote_asset_volume = table.Column<decimal>(nullable: false),
                    final = table.Column<bool>(nullable: false),
                    trade_count = table.Column<int>(nullable: false),
                    volume = table.Column<decimal>(nullable: false),
                    low = table.Column<decimal>(nullable: false),
                    high = table.Column<decimal>(nullable: false),
                    close = table.Column<decimal>(nullable: false),
                    taker_buy_quote_asset_volume = table.Column<decimal>(nullable: false),
                    last_trade = table.Column<long>(nullable: false),
                    first_trade = table.Column<long>(nullable: false),
                    interval = table.Column<int>(nullable: false),
                    symbol = table.Column<string>(nullable: true),
                    close_time = table.Column<DateTime>(nullable: false),
                    open_time = table.Column<DateTime>(nullable: false),
                    taker_buy_base_asset_volume = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kline", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_list",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_list_id = table.Column<long>(nullable: false),
                    contingency_type = table.Column<string>(nullable: true),
                    list_status_type = table.Column<int>(nullable: false),
                    list_order_status = table.Column<int>(nullable: false),
                    list_client_order_id = table.Column<string>(nullable: true),
                    transaction_time = table.Column<DateTime>(nullable: false),
                    symbol = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_list", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "symbol",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(nullable: true),
                    is_collection_active = table.Column<bool>(nullable: false),
                    is_trading_active = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_symbol", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tick",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    total_trades = table.Column<long>(nullable: false),
                    last_trade_id = table.Column<long>(nullable: false),
                    first_trade_id = table.Column<long>(nullable: false),
                    total_traded_quote_asset_volume = table.Column<decimal>(nullable: false),
                    total_traded_base_asset_volume = table.Column<decimal>(nullable: false),
                    low_price = table.Column<decimal>(nullable: false),
                    high_price = table.Column<decimal>(nullable: false),
                    open_price = table.Column<decimal>(nullable: false),
                    best_ask_quantity = table.Column<decimal>(nullable: false),
                    best_ask_price = table.Column<decimal>(nullable: false),
                    best_bid_quantity = table.Column<decimal>(nullable: false),
                    best_bid_price = table.Column<decimal>(nullable: false),
                    close_trades_quantity = table.Column<decimal>(nullable: false),
                    current_day_close_price = table.Column<decimal>(nullable: false),
                    prev_day_close_price = table.Column<decimal>(nullable: false),
                    weighted_average = table.Column<decimal>(nullable: false),
                    price_change_percentage = table.Column<decimal>(nullable: false),
                    price_change = table.Column<decimal>(nullable: false),
                    symbol = table.Column<string>(nullable: true),
                    statistics_open_time = table.Column<DateTime>(nullable: false),
                    statistics_close_time = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tick", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "balance",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asset = table.Column<string>(nullable: true),
                    free = table.Column<decimal>(nullable: false),
                    locked = table.Column<decimal>(nullable: false),
                    created_on = table.Column<DateTime>(nullable: false),
                    account_info_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_balance", x => x.id);
                    table.ForeignKey(
                        name: "FK_balance_account_info_account_info_id",
                        column: x => x.account_info_id,
                        principalTable: "account_info",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    symbol = table.Column<string>(nullable: true),
                    order_id = table.Column<long>(nullable: false),
                    client_order_id = table.Column<string>(nullable: true),
                    order_list_id = table.Column<long>(nullable: false),
                    created_on = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_order_list_order_list_id",
                        column: x => x.order_list_id,
                        principalTable: "order_list",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_update",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    last_quote_transacted_quantity = table.Column<decimal>(nullable: false),
                    quote_order_quantity = table.Column<decimal>(nullable: false),
                    cummulative_quote_quantity = table.Column<decimal>(nullable: false),
                    order_creation_time = table.Column<DateTime>(nullable: false),
                    buyer_is_maker = table.Column<bool>(nullable: false),
                    is_working = table.Column<bool>(nullable: false),
                    trade_id = table.Column<long>(nullable: false),
                    created_on = table.Column<DateTime>(nullable: false),
                    commission_asset = table.Column<string>(nullable: true),
                    commission = table.Column<decimal>(nullable: false),
                    price_last_filled_trade = table.Column<decimal>(nullable: false),
                    accumulated_quantity_of_filled_trades = table.Column<decimal>(nullable: false),
                    quantity_of_last_filled_trade = table.Column<decimal>(nullable: false),
                    order_id = table.Column<long>(nullable: false),
                    reject_reason = table.Column<int>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    execution_type = table.Column<int>(nullable: false),
                    original_client_order_id = table.Column<string>(nullable: true),
                    iceberg_quantity = table.Column<decimal>(nullable: false),
                    stop_price = table.Column<decimal>(nullable: false),
                    price = table.Column<decimal>(nullable: false),
                    quantity = table.Column<decimal>(nullable: false),
                    time_in_force = table.Column<int>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    side = table.Column<int>(nullable: false),
                    client_order_id = table.Column<string>(nullable: true),
                    symbol = table.Column<string>(nullable: true),
                    order_list_id = table.Column<long>(nullable: true),
                    i = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_update", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_update_order_order_id",
                        column: x => x.order_id,
                        principalTable: "order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_update_order_list_order_list_id",
                        column: x => x.order_list_id,
                        principalTable: "order_list",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_info_id",
                table: "account_info",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_balance_account_info_id",
                table: "balance",
                column: "account_info_id");

            migrationBuilder.CreateIndex(
                name: "IX_balance_id",
                table: "balance",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_balance_update_id",
                table: "balance_update",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_book_tick_update_id",
                table: "book_tick",
                column: "update_id");

            migrationBuilder.CreateIndex(
                name: "IX_book_tick_created_on_symbol",
                table: "book_tick",
                columns: new[] { "created_on", "symbol" });

            migrationBuilder.CreateIndex(
                name: "IX_kline_id",
                table: "kline",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_kline_open_time_interval_symbol",
                table: "kline",
                columns: new[] { "open_time", "interval", "symbol" });

            migrationBuilder.CreateIndex(
                name: "IX_order_client_order_id",
                table: "order",
                column: "client_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_id",
                table: "order",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_order_order_id",
                table: "order",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_order_list_id",
                table: "order",
                column: "order_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_list_order_list_id",
                table: "order_list",
                column: "order_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_list_symbol",
                table: "order_list",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "IX_order_update_client_order_id",
                table: "order_update",
                column: "client_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_update_id",
                table: "order_update",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_order_update_order_id",
                table: "order_update",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_update_order_list_id",
                table: "order_update",
                column: "order_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_update_original_client_order_id",
                table: "order_update",
                column: "original_client_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_update_symbol_side",
                table: "order_update",
                columns: new[] { "symbol", "side" });

            migrationBuilder.CreateIndex(
                name: "IX_tick_id",
                table: "tick",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_tick_statistics_open_time_statistics_close_time",
                table: "tick",
                columns: new[] { "statistics_open_time", "statistics_close_time" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "balance");

            migrationBuilder.DropTable(
                name: "balance_update");

            migrationBuilder.DropTable(
                name: "book_tick");

            migrationBuilder.DropTable(
                name: "kline");

            migrationBuilder.DropTable(
                name: "order_update");

            migrationBuilder.DropTable(
                name: "symbol");

            migrationBuilder.DropTable(
                name: "tick");

            migrationBuilder.DropTable(
                name: "account_info");

            migrationBuilder.DropTable(
                name: "order");

            migrationBuilder.DropTable(
                name: "order_list");
        }
    }
}
