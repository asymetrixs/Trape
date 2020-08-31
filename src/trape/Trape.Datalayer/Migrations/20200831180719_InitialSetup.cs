using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Trape.Datalayer.Migrations
{
    public partial class InitialSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "stubs");

            migrationBuilder.CreateTable(
                name: "account_infos",
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
                    table.PrimaryKey("PK_account_infos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "balance_updates",
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
                    table.PrimaryKey("PK_balance_updates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "book_ticks",
                columns: table => new
                {
                    update_id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    symbol = table.Column<string>(nullable: true),
                    best_bid_price = table.Column<decimal>(nullable: false),
                    best_bid_quantity = table.Column<decimal>(nullable: false),
                    best_ask_price = table.Column<decimal>(nullable: false),
                    best_ask_quantity = table.Column<decimal>(nullable: false),
                    transaction_time = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_ticks", x => x.update_id);
                });

            migrationBuilder.CreateTable(
                name: "client_order",
                columns: table => new
                {
                    id = table.Column<string>(nullable: false),
                    created_on = table.Column<DateTime>(nullable: false),
                    symbol = table.Column<string>(nullable: true),
                    side = table.Column<int>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    quantity = table.Column<decimal>(nullable: false),
                    price = table.Column<decimal>(nullable: false),
                    order_id = table.Column<int>(nullable: true),
                    order_response_type = table.Column<int>(nullable: false),
                    time_in_force = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_order", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "klines",
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
                    last_trade_id = table.Column<long>(nullable: false),
                    first_trade_id = table.Column<long>(nullable: false),
                    interval = table.Column<int>(nullable: false),
                    symbol = table.Column<string>(nullable: true),
                    close_time = table.Column<DateTime>(nullable: false),
                    open_time = table.Column<DateTime>(nullable: false),
                    taker_buy_base_asset_volume = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_klines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_lists",
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
                    table.PrimaryKey("PK_order_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "placed_orders",
                columns: table => new
                {
                    order_id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    margin_buy_borrow_asset = table.Column<string>(nullable: true),
                    create_time = table.Column<DateTime>(nullable: false),
                    margin_buy_borrow_amount = table.Column<decimal>(nullable: true),
                    stop_price = table.Column<decimal>(nullable: true),
                    side = table.Column<int>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    time_in_force = table.Column<int>(nullable: false),
                    status = table.Column<int>(nullable: false),
                    quote_quantity = table.Column<decimal>(nullable: false),
                    quote_quantity_filled = table.Column<decimal>(nullable: false),
                    quantity_filled = table.Column<decimal>(nullable: false),
                    quantity = table.Column<decimal>(nullable: false),
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
                name: "symbols",
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
                    table.PrimaryKey("PK_symbols", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ticks",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    total_trades = table.Column<long>(nullable: false),
                    close_time = table.Column<DateTime>(nullable: false),
                    open_time = table.Column<DateTime>(nullable: false),
                    last_trade_id = table.Column<long>(nullable: false),
                    first_trade_id = table.Column<long>(nullable: false),
                    total_traded_quote_asset_volume = table.Column<decimal>(nullable: false),
                    total_traded_base_asset_volume = table.Column<decimal>(nullable: false),
                    low_price = table.Column<decimal>(nullable: false),
                    high_price = table.Column<decimal>(nullable: false),
                    open_price = table.Column<decimal>(nullable: false),
                    ask_quantity = table.Column<decimal>(nullable: false),
                    ask_price = table.Column<decimal>(nullable: false),
                    bid_quantity = table.Column<decimal>(nullable: false),
                    bid_price = table.Column<decimal>(nullable: false),
                    last_quantity = table.Column<decimal>(nullable: false),
                    last_price = table.Column<decimal>(nullable: false),
                    prev_day_close_price = table.Column<decimal>(nullable: false),
                    weighted_average_price = table.Column<decimal>(nullable: false),
                    price_change_percent = table.Column<decimal>(nullable: false),
                    price_change = table.Column<decimal>(nullable: false),
                    symbol = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "last_decisions",
                schema: "stubs",
                columns: table => new
                {
                    r_symbol = table.Column<string>(nullable: true),
                    r_action = table.Column<int>(type: "int4", nullable: false),
                    r_event_time = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "latest_ma10m_and_ma30m_crossing",
                schema: "stubs",
                columns: table => new
                {
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "latest_ma1h_and_ma3h_crossing",
                schema: "stubs",
                columns: table => new
                {
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "latest_ma30m_and_ma1h_crossing",
                schema: "stubs",
                columns: table => new
                {
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "stats10m",
                schema: "stubs",
                columns: table => new
                {
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
                });

            migrationBuilder.CreateTable(
                name: "stats15s",
                schema: "stubs",
                columns: table => new
                {
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
                });

            migrationBuilder.CreateTable(
                name: "stats2h",
                schema: "stubs",
                columns: table => new
                {
                    r_symbol = table.Column<string>(nullable: true),
                    r_databasis = table.Column<int>(nullable: false),
                    r_slope_6h = table.Column<decimal>(nullable: false),
                    r_slope_12h = table.Column<decimal>(nullable: false),
                    r_slope_18h = table.Column<decimal>(nullable: false),
                    r_slope_1d = table.Column<decimal>(nullable: false),
                    r_movav_6h = table.Column<decimal>(nullable: false),
                    r_movav_12h = table.Column<decimal>(nullable: false),
                    r_movav_18h = table.Column<decimal>(nullable: false),
                    r_movav_1d = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "stats2m",
                schema: "stubs",
                columns: table => new
                {
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
                });

            migrationBuilder.CreateTable(
                name: "stats3s",
                schema: "stubs",
                columns: table => new
                {
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
                });

            migrationBuilder.CreateTable(
                name: "balances",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asset = table.Column<string>(nullable: true),
                    free = table.Column<decimal>(nullable: false),
                    locked = table.Column<decimal>(nullable: false),
                    total = table.Column<decimal>(nullable: false),
                    created_on = table.Column<DateTime>(nullable: false),
                    account_info_id = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_balances", x => x.id);
                    table.ForeignKey(
                        name: "FK_balances_account_infos_account_info_id",
                        column: x => x.account_info_id,
                        principalTable: "account_infos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
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
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_orders_client_order_client_order_id",
                        column: x => x.client_order_id,
                        principalTable: "client_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_order_lists_order_list_id",
                        column: x => x.order_list_id,
                        principalTable: "order_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "order_updates",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    last_quote_quantity = table.Column<decimal>(nullable: false),
                    create_time = table.Column<DateTime>(nullable: false),
                    last_price_filled = table.Column<decimal>(nullable: false),
                    quote_order_quantity = table.Column<decimal>(nullable: false),
                    buyer_is_maker = table.Column<bool>(nullable: false),
                    is_working = table.Column<bool>(nullable: false),
                    trade_id = table.Column<long>(nullable: false),
                    commission_asset = table.Column<string>(nullable: true),
                    commission = table.Column<decimal>(nullable: false),
                    last_quantity_filled = table.Column<decimal>(nullable: false),
                    quantity_filled = table.Column<decimal>(nullable: false),
                    order_id = table.Column<long>(nullable: false),
                    quote_quantity = table.Column<decimal>(nullable: false),
                    update_time = table.Column<DateTime>(nullable: false),
                    quote_quantity_filled = table.Column<decimal>(nullable: false),
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
                    table.PrimaryKey("PK_order_updates", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_updates_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_updates_order_lists_order_list_id",
                        column: x => x.order_list_id,
                        principalTable: "order_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_infos_id",
                table: "account_infos",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_balance_updates_id",
                table: "balance_updates",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_balances_account_info_id",
                table: "balances",
                column: "account_info_id");

            migrationBuilder.CreateIndex(
                name: "IX_balances_id",
                table: "balances",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_book_ticks_update_id",
                table: "book_ticks",
                column: "update_id");

            migrationBuilder.CreateIndex(
                name: "IX_book_ticks_transaction_time_symbol",
                table: "book_ticks",
                columns: new[] { "transaction_time", "symbol" });

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
                name: "IX_klines_id",
                table: "klines",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_klines_open_time_interval_symbol",
                table: "klines",
                columns: new[] { "open_time", "interval", "symbol" });

            migrationBuilder.CreateIndex(
                name: "IX_order_lists_order_list_id",
                table: "order_lists",
                column: "order_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_lists_symbol",
                table: "order_lists",
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
                name: "IX_order_updates_client_order_id",
                table: "order_updates",
                column: "client_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_updates_id",
                table: "order_updates",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_order_updates_order_id",
                table: "order_updates",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_updates_order_list_id",
                table: "order_updates",
                column: "order_list_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_updates_original_client_order_id",
                table: "order_updates",
                column: "original_client_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_updates_symbol_side",
                table: "order_updates",
                columns: new[] { "symbol", "side" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_client_order_id",
                table: "orders",
                column: "client_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_id",
                table: "orders",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_id",
                table: "orders",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_list_id",
                table: "orders",
                column: "order_list_id");

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
                name: "IX_ticks_id",
                table: "ticks",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_ticks_open_time_close_time",
                table: "ticks",
                columns: new[] { "open_time", "close_time" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "balance_updates");

            migrationBuilder.DropTable(
                name: "balances");

            migrationBuilder.DropTable(
                name: "book_ticks");

            migrationBuilder.DropTable(
                name: "klines");

            migrationBuilder.DropTable(
                name: "order_trades");

            migrationBuilder.DropTable(
                name: "order_updates");

            migrationBuilder.DropTable(
                name: "recommendations");

            migrationBuilder.DropTable(
                name: "symbols");

            migrationBuilder.DropTable(
                name: "ticks");

            migrationBuilder.DropTable(
                name: "last_decisions",
                schema: "stubs");

            migrationBuilder.DropTable(
                name: "latest_ma10m_and_ma30m_crossing",
                schema: "stubs");

            migrationBuilder.DropTable(
                name: "latest_ma1h_and_ma3h_crossing",
                schema: "stubs");

            migrationBuilder.DropTable(
                name: "latest_ma30m_and_ma1h_crossing",
                schema: "stubs");

            migrationBuilder.DropTable(
                name: "stats10m",
                schema: "stubs");

            migrationBuilder.DropTable(
                name: "stats15s",
                schema: "stubs");

            migrationBuilder.DropTable(
                name: "stats2h",
                schema: "stubs");

            migrationBuilder.DropTable(
                name: "stats2m",
                schema: "stubs");

            migrationBuilder.DropTable(
                name: "stats3s",
                schema: "stubs");

            migrationBuilder.DropTable(
                name: "account_infos");

            migrationBuilder.DropTable(
                name: "placed_orders");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "client_order");

            migrationBuilder.DropTable(
                name: "order_lists");
        }
    }
}
