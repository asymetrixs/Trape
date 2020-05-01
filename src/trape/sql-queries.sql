SELECT bpo.time_in_force, bpo.transaction_time, bpo.side, bpo.symbol, bot.price, bot.quantity, bot.commission, 
	bot.commission_asset, bot.consumed, ROUND(bot.consumed_price, 8),
	ROUND(bot.price * bot.quantity) payed, ROUND(bot.consumed_price * bot.consumed) got,
	ROUND(bot.consumed_price * bot.consumed - bot.price * bot.quantity) win
	FROM binance_order_trade AS bot
LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
ORDER BY binance_placed_order_id DESC, bpo.id DESC;

WITH data AS (
	SELECT transaction_time::DATE,
		ROUND(SUM(price * quantity) FILTER (WHERE side = 'Buy'), 8) AS buy,
		ROUND(SUM(price * quantity) FILTER (WHERE side = 'Sell'), 8) AS sell,
		ROUND(SUM(price * quantity) FILTER (WHERE side = 'Sell') - SUM(price * quantity) FILTER (WHERE side = 'Buy'), 8) AS profit
		FROM (
			SELECT bpo.time_in_force, bpo.transaction_time, bpo.side, bpo.symbol, bot.price, bot.quantity, bot.commission, 
				bot.commission_asset, bot.consumed, ROUND(bot.consumed_price, 8),
				ROUND(bot.price * bot.quantity) payed, ROUND(bot.consumed_price * bot.consumed) got,
				ROUND(bot.consumed_price * bot.consumed - bot.price * bot.quantity) win
				FROM binance_order_trade AS bot
			LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
			WHERE symbol = 'BTCUSDT'
		) a	
		GROUP BY transaction_time::DATE
		ORDER BY transaction_time::DATE
	)
SELECT transaction_time, buy, sell, profit, LAG(profit, 1) OVER (ORDER BY transaction_time ASC), 
	profit + LAG(profit, 1) OVER (ORDER BY transaction_time ASC) AS cleaned
	FROM data

select buy.min as start, ROUND(buy.sum, 2) AS bought, ROUND(sell.sum, 2) AS sold, ROUND(sell.sum - buy.sum, 2) AS profit FROM
(select min(transaction_time), sum(bot.price * quantity) from binance_order_trade bot
LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
where side = 'Buy') buy,
(select sum(bot.price * quantity) from binance_order_trade bot
LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
where side = 'Sell') sell

SELECT AVG(sums) FROM (
select SUM(bot.price * quantity) AS sums from binance_order_trade bot
LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
where side = 'Buy'
GROUP BY binance_placed_order_id)a

select * from select_asset_status()
select * from current_statement()
select * from report_walking_profit('BTCUSDT')
select * From report_profits('BTCUSDT')
select * from report_last_decisions() ORDER BY r_event_time DESC
select * from select_last_orders('BTCUSDT')

--"2020-04-23"	221.05418804
--"2020-04-24"	252.19632958
--"2020-04-25"	250.26010998

select * from binance_order_trade order by binance_placed_order_id desc
update binance_order_trade set consumed = quantity, consumed_price = 6630 where consumed != quantity
update binance_order_trade set consumed_price = price where binance_placed_order_id != 278 AND consumed_price = 0
select * from binance_order_trade

delete from binance_placed_order;
delete from binance_order_trade;

SELECT * FROM fix_symbol_quantity('BTCUSDT', 0.04132487, 7133.00);

