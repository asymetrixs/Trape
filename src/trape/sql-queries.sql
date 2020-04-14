SELECT bpo.time_in_force, bpo.transaction_time, bpo.side, bpo.symbol, bot.price, bot.quantity, bot.commission, 
	bot.commission_asset, bot.consumed, ROUND(bot.consumed_price, 8) FROM binance_order_trade AS bot
LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
ORDER BY binance_placed_order_id DESC, bpo.id DESC;

SELECT transaction_time, symbol, ROUND(buy, 8), ROUND(sell, 8), ROUND(sell-buy, 8) AS profit FROM (
	SELECT	transaction_time::DATE,
			symbol,
			SUM(bot.price*consumed) as buy,
			SUM(consumed_price*consumed) as sell
			FROM binance_order_trade bot
			INNER JOIN binance_placed_order bop ON bop.id = bot.binance_placed_order_id
	WHERE side = 'Buy'
	GROUP BY transaction_time::DATE, symbol ) a
ORDER BY transaction_time::DATE DESC, symbol ASC

update binance_order_trade set consumed = quantity where binance_placed_order_id != 278
update binance_order_trade set consumed_price = price where binance_placed_order_id != 278 AND consumed_price = 0
select * from binance_order_trade

select * from select_asset_status()
--233
--	Bitcoin 0.44544746 0.44544746
-- Ethereum 8.16653543 8.16653543

delete from binance_order_trade where binance_placed_order_id = 226;
delete from binance_placed_order where id = 226;

select * from recommendation where event_time > '2020-04-12 12:14:00 +00'::timestamptz AND  event_time < '2020-04-12 12:18:00 +00'::timestamptz
	AND slope10m < -0.004 AND slope15m < -0.001
	
select event_time, slope1h from recommendation where event_time > '2020-04-12 14:45:00 +00'::timestamptz AND  event_time < '2020-04-12 14:46:00 +00'::timestamptz
ORDER BY id ASC

select * from recommendation order by id desc limit 10

select * from binance_order_trade order by binance_placed_order_id DESC;

select * from binance_order_trade
select * from "order" order by id desc;
select * from stats_2h()

select * from select_last_orders('ETHUSDT')
select * from binance_placed_order
--delete from binance_placed_order;delete from binance_order_trade;

SELECT max(id) from binance_placed_order



delete from binance_placed_order;
delete from binance_order_trade;
SELECT * FROM fix_symbol_quantity('BTCUSDT', 0.04132487, 7133.00);










'2020-04-10 05:00:00.000 +00'::TIMESTAMPTZ
SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= '2020-04-10 05:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes'))::NUMERIC, 8) AS slope_30m,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >='2020-04-10 05:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hour'))::NUMERIC, 8) AS slope_1h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= '2020-04-10 05:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours'))::NUMERIC, 8) AS slope_2h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_3h,
		ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 05:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 05:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes')), 8) AS movav_30m,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 05:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 05:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hours')), 8) AS movav_1h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 05:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 05:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours')), 8) AS movav_2h,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_3h
	FROM binance_stream_tick
	WHERE event_time BETWEEN '2020-04-10 03:00:00.000 +00'::TIMESTAMPTZ AND '2020-04-10 05:00:00.000 +00'::TIMESTAMPTZ
	GROUP BY symbol;


'2020-04-10 05:30:00.000 +00'::TIMESTAMPTZ
SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= '2020-04-10 05:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes'))::NUMERIC, 8) AS slope_30m,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >='2020-04-10 05:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hour'))::NUMERIC, 8) AS slope_1h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= '2020-04-10 05:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours'))::NUMERIC, 8) AS slope_2h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_3h,
		ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 05:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 05:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes')), 8) AS movav_30m,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 05:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 05:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hours')), 8) AS movav_1h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 05:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 05:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours')), 8) AS movav_2h,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_3h
	FROM binance_stream_tick
	WHERE event_time BETWEEN '2020-04-10 03:30:00.000 +00'::TIMESTAMPTZ AND '2020-04-10 05:30:00.000 +00'::TIMESTAMPTZ
	GROUP BY symbol;


'2020-04-10 06:00:00.000 +00'::TIMESTAMPTZ
SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= '2020-04-10 06:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes'))::NUMERIC, 8) AS slope_30m,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >='2020-04-10 06:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hour'))::NUMERIC, 8) AS slope_1h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= '2020-04-10 06:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours'))::NUMERIC, 8) AS slope_2h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_3h,
		ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 06:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 06:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes')), 8) AS movav_30m,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 06:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 06:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hours')), 8) AS movav_1h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 06:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 06:00:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours')), 8) AS movav_2h,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_3h
	FROM binance_stream_tick
	WHERE event_time BETWEEN '2020-04-10 04:00:00.000 +00'::TIMESTAMPTZ AND '2020-04-10 06:00:00.000 +00'::TIMESTAMPTZ
	GROUP BY symbol;
	

'2020-04-10 06:30:00.000 +00'::TIMESTAMPTZ
SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= '2020-04-10 06:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes'))::NUMERIC, 8) AS slope_30m,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >='2020-04-10 06:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hour'))::NUMERIC, 8) AS slope_1h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= '2020-04-10 06:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours'))::NUMERIC, 8) AS slope_2h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_3h,
		ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 06:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 06:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '30 minutes')), 8) AS movav_30m,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 06:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 06:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '1 hours')), 8) AS movav_1h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= '2020-04-10 06:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= '2020-04-10 06:30:00.000 +00'::TIMESTAMPTZ - INTERVAL '2 hours')), 8) AS movav_2h,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_3h
	FROM binance_stream_tick
	WHERE event_time BETWEEN '2020-04-10 04:30:00.000 +00'::TIMESTAMPTZ AND '2020-04-10 06:30:00.000 +00'::TIMESTAMPTZ
	GROUP BY symbol;