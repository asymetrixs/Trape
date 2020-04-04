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

UPDATE binance_order_trade SET consumed = quantity;
INSERT INTO binance_placed_order (side, type, time_in_force, status, original_quote_order_quantity, 
								 cumulative_quote_quantity, executed_quantity, original_quantity,
								 price, order_id, symbol, transaction_time, original_client_order_id,
								 client_order_id)
					 VALUES ('Buy', 'Market', 'GoodTillCancel', 'Filled', 0, 0, 0, 0, 0,
								0, 'ETHUSDT', now(), 0, 'fix') RETURNING id;
INSERT INTO binance_order_trade (binance_placed_order_id, trade_id, price, quantity, commission, commission_asset)
	VALUES (131, -1, 141, 3.05525728, 0, 'BNB');
	
INSERT INTO binance_placed_order (side, type, time_in_force, status, original_quote_order_quantity, 
								 cumulative_quote_quantity, executed_quantity, original_quantity,
								 price, order_id, symbol, transaction_time, original_client_order_id,
								 client_order_id)
					 VALUES ('Buy', 'Market', 'GoodTillCancel', 'Filled', 0, 0, 0, 0, 0,
								0, 'BTCUSDT', now(), 0, 'fix') RETURNING id;
INSERT INTO binance_order_trade (binance_placed_order_id, trade_id, price, quantity, commission, commission_asset)
	VALUES (132, -1, 6750, 0.10656686, 0, 'BNB');

update binance_order_trade set quantity = 0.13755765 where binance_placed_order_id = 132;

UPDATE binance_order_trade SET consumed = quantity WHERE binance_placed_order_id < 131
select * from select_asset_status()
