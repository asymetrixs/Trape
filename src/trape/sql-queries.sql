SELECT bpo.time_in_force, bpo.transaction_time, bpo.side, bpo.symbol, bot.* FROM binance_order_trade bot
LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
ORDER BY binance_placed_order_id DESC, id DESC;

select * from binance_order_trade
select * from "order" order by id desc;
select * from stats_2h()

select * from select_last_orders('ETHUSDT')
select * from binance_placed_order
--delete from binance_placed_order;delete from binance_order_trade;

SELECT max(id) from binance_placed_order



SELECT * FROM fix_symbol_quantity('ETHUSDT', 7.92182856, 141.44);

select * from select_asset_status()
