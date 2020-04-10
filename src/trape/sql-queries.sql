SELECT bpo.time_in_force, bpo.transaction_time, bpo.side, bpo.symbol, bot.price, bot.quantity, bot.commission, 
	bot.commission_asset, bot.consumed, ROUND(bot.current_price, 8) FROM binance_order_trade bot
LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
ORDER BY binance_placed_order_id DESC, id DESC;

select * from recommendation order by event_time desc limit 100

SELECT transaction_time, symbol, ROUND(buy, 8), ROUND(sell, 8), ROUND(sell-buy, 8) AS profit FROM (
	SELECT	transaction_time::DATE,
			symbol,
			SUM(bot.price*consumed) as buy,
			SUM(consumed_price*consumed) as sell
			FROM binance_order_trade bot
			INNER JOIN binance_placed_order bop ON bop.id = bot.binance_placed_order_id
	WHERE consumed != 0
	GROUP BY transaction_time::DATE, symbol ) a
ORDER BY transaction_time::DATE DESC, symbol ASC

select * from select_asset_status()
--233
--	Bitcoin 0.44544746 0.44544746
-- Ethereum 8.16653543 8.16653543

delete from binance_order_trade where binance_placed_order_id = 226;
delete from binance_placed_order where id = 226;



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
SELECT * FROM fix_symbol_quantity('BTCUSDT', 0.01053939, 7343.58);
SELECT * FROM fix_symbol_quantity('ETHUSDT', 0, 170.59);









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