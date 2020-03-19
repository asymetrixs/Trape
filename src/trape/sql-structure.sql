--drop table binance_stream_tick
CREATE TABLE binance_stream_tick
(
	id bigserial not null,
	event text not null,
	event_time timestamptz not null,
	total_trades int8 not null,
	last_trade_id int8 not null,
	first_trade_id int8 not null,
	total_traded_quote_asset_volume numeric not null,
	total_traded_base_asset_volume numeric not null,
	low_price numeric not null,
	high_price numeric not null,
	open_price numeric not null,
	best_ask_quantity numeric not null,
	best_ask_price numeric not null,
	best_bid_quantity numeric not null,
	best_bid_price numeric not null,
	close_trades_quantity numeric not null,
	current_day_close_price numeric not null,
	prev_day_close_price numeric not null,
	weighted_average numeric not null,
	price_change_percentage numeric not null,
	price_change numeric not null,
	symbol text not null,
	statistics_open_time timestamptz not null,
	statistics_close_time timestamptz not null,
	PRIMARY KEY (id)
);
--DROP INDEX uq_bst_cs;
CREATE INDEX ix_bst_cs ON binance_stream_tick (event_time, symbol);

--DROP FUNCTION insert_binance_stream_tick;
CREATE OR REPLACE FUNCTION insert_binance_stream_tick (
	p_event text,
	p_event_time timestamptz,
	p_total_trades int8,
	p_last_trade_id int8,
	p_first_trade_id int8,
	p_total_traded_quote_asset_volume numeric,
	p_total_traded_base_asset_volume numeric,
	p_low_price numeric,
	p_high_price numeric,
	p_open_price numeric,
	p_best_ask_quantity numeric,
	p_best_ask_price numeric,
	p_best_bid_quantity numeric,
	p_best_bid_price numeric,
	p_close_trades_quantity numeric,
	p_current_day_close_price numeric,
	p_prev_day_close_price numeric,
	p_weighted_average numeric,
	p_price_change_percentage numeric,
	p_price_change numeric,
	p_symbol text,
	p_statistics_open_time timestamptz,
	p_statistics_close_time timestamptz
)
	RETURNS void AS
$$
BEGIN
	INSERT INTO binance_stream_tick (
		event,
		event_time,
		total_trades,
		last_trade_id,
		first_trade_id,
		total_traded_quote_asset_volume,
		total_traded_base_asset_volume,
		low_price,
		high_price,
		open_price,
		best_ask_quantity,
		best_ask_price,
		best_bid_quantity,
		best_bid_price,
		close_trades_quantity,
		current_day_close_price,
		prev_day_close_price,
		weighted_average,
		price_change_percentage,
		price_change,
		symbol,
		statistics_open_time,
		statistics_close_time
	) VALUES (
		p_event,
		p_event_time,
		p_total_trades,
		p_last_trade_id,
		p_first_trade_id,
		p_total_traded_quote_asset_volume,
		p_total_traded_base_asset_volume,
		p_low_price,
		p_high_price,
		p_open_price,
		p_best_ask_quantity,
		p_best_ask_price,
		p_best_bid_quantity,
		p_best_bid_price,
		p_close_trades_quantity,
		p_current_day_close_price,
		p_prev_day_close_price,
		p_weighted_average,
		p_price_change_percentage,
		p_price_change,
		p_symbol,
		p_statistics_open_time,
		p_statistics_close_time
	) ON CONFLICT DO NOTHING;

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;


select * from binance_stream_tick;



CREATE TABLE binance_stream_kline_data
(
	id bigserial not null,
	event text not null,
	event_time timestamptz not null,
	close numeric not null,
	close_time timestamptz not null,
	final boolean not null,
	first_trade_id int8 not null,
	high_price numeric not null,
	interval text not null,
	last_trade_id int8 not null,
	low_price numeric not null,
	open_price numeric not null,
	open_time timestamptz not null,
	quote_asset_volume numeric not null,
	symbol text not null,
	taker_buy_base_asset_volume numeric not null,
	taker_buy_quote_asset_volume numeric not null,
	trade_count int4 not null,
	volume numeric not null,	
	PRIMARY KEY (id)
);
--DROP INDEX uq_bst_cs;
CREATE INDEX ix_bskd_cs ON binance_stream_kline_data (event_time, symbol, interval);
CREATE UNIQUE INDEX uq_bskd_fi ON binance_stream_kline_data (first_trade_id, interval);


--DROP FUNCTION insert_binance_stream_kline_data;
CREATE OR REPLACE FUNCTION insert_binance_stream_kline_data (
	p_event text,
	p_event_time timestamptz,
	p_close numeric,
	p_close_time timestamptz,
	p_final boolean,
	p_first_trade_id int8,
	p_high_price numeric,
	p_interval text,
	p_last_trade_id int8,
	p_low_price numeric,
	p_open_price numeric,
	p_open_time timestamptz,
	p_quote_asset_volume numeric,
	p_symbol text,
	p_taker_buy_base_asset_volume numeric,
	p_taker_buy_quote_asset_volume numeric,
	p_trade_count int4,
	p_volume numeric
)
	RETURNS void AS
$$
BEGIN
	INSERT INTO binance_stream_kline_data (
		event,
		event_time,
		close,
		close_time,
		final,
		first_trade_id,
		high_price,
		interval,
		last_trade_id,
		low_price,
		open_price,
		open_time,
		quote_asset_volume,
		symbol,
		taker_buy_base_asset_volume,
		taker_buy_quote_asset_volume,
		trade_count,
		volume
	) VALUES (
		p_event,
		p_event_time,
		p_close,
		p_close_time,
		p_final,
		p_first_trade_id,
		p_high_price,
		p_interval,
		p_last_trade_id,
		p_low_price,
		p_open_price,
		p_open_time,
		p_quote_asset_volume,
		p_symbol,
		p_taker_buy_base_asset_volume,
		p_taker_buy_quote_asset_volume,
		p_trade_count,
		p_volume
	) ON CONFLICT (first_trade_id, interval) DO UPDATE SET
		event_time = p_event_time,
		close = p_close,
		close_time = p_close_time,
		final = p_final,
		high_price = p_high_price,
		last_trade_id = p_last_trade_id,
		low_price = p_low_price,
		quote_asset_volume = p_quote_asset_volume,
		taker_buy_base_asset_volume = p_taker_buy_base_asset_volume,
		taker_buy_quote_asset_volume = p_taker_buy_quote_asset_volume,
		trade_count = p_trade_count,
		volume = p_volume;

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;




CREATE TABLE binance_book_tick
(
	update_id int8 not null,
	symbol text not null,
	event_time timestamptz not null,
	best_ask_price numeric not null,
	best_ask_quantity numeric not null,
	best_bid_price numeric not null,
	best_bid_quantity numeric not null,
	PRIMARY KEY (update_id)
);

CREATE INDEX ix_bbt_ets ON binance_book_tick (event_time, symbol);
CREATE INDEX ix_bbt_et ON binance_book_tick USING BRIN (event_time);

--DROP FUNCTION insert_binance_book_tick;
CREATE OR REPLACE FUNCTION insert_binance_book_tick (
	p_update_id int8,
	p_symbol text,
	p_event_time timestamptz,
	p_best_ask_price numeric,
	p_best_ask_quantity numeric,
	p_best_bid_price numeric,
	p_best_bid_quantity numeric
)
	RETURNS void AS
$$
BEGIN
	INSERT INTO binance_book_tick (
		update_id,
		symbol,
		event_time,
		best_ask_price,
		best_ask_quantity,
		best_bid_price,
		best_bid_quantity
	) VALUES (
		p_update_id,
		p_symbol,
		p_event_time,
		p_best_ask_price,
		p_best_ask_quantity,
		p_best_bid_price,
		p_best_bid_quantity
	) ON CONFLICT DO NOTHING;

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;


CREATE OR REPLACE FUNCTION cleanup_book_ticks()
RETURNS int4 AS
$$
	DECLARE i_deleted int4;
BEGIN
	WITH deleted AS (DELETE FROM binance_book_tick WHERE event_time < NOW() - INTERVAL '48 hours' RETURNING *)
	SELECT COUNT(*) INTO i_deleted FROM deleted;
	
	RETURN i_deleted;
	
END;
$$
LANGUAGE plpgsql VOLATILE STRICT;



CREATE OR REPLACE FUNCTION current_price(p_symbol TEXT)
RETURNS NUMERIC AS
$$
	DECLARE r_price NUMERIC;
BEGIN
	SELECT current_day_close_price INTO r_price FROM binance_stream_tick
	WHERE symbol = p_symbol
	ORDER BY event_time DESC
	LIMIT 1;

	RETURN r_price;
END;
$$
LANGUAGE plpgsql STRICT;

select 

--delete from decision where id = 43;
--select * from decision order by id desc;
--select symbol, decision, SUM(price)
--from decision group by symbol, decision;
--select * from current_price()
CREATE OR REPLACE FUNCTION current_price()
RETURNS TABLE 
(
	r_symbol TEXT,
	r_event_time TIMESTAMPTZ,
	r_low_price NUMERIC,
	r_high_price NUMERIC,
	r_open_price NUMERIC,
	r_current_day_close_price NUMERIC,
	r_price_change_percentage NUMERIC,
	r_price_change NUMERIC
)
AS
$$
BEGIN
	RETURN QUERY SELECT DISTINCT ON (symbol) symbol, event_time, low_price, high_price, open_price,
		current_day_close_price, price_change_percentage, price_change
		FROM binance_stream_tick
		WHERE event_time > NOW() - INTERVAL '5 seconds'
		GROUP BY symbol, event_time, symbol, low_price, high_price, open_price, current_day_close_price, price_change_percentage, price_change
		ORDER BY symbol, event_time DESC;
END;
$$
LANGUAGE plpgsql STRICT;


CREATE TABLE decision
(
	id bigserial NOT NULL,
	event_time TIMESTAMPTZ DEFAULT NOW() NOT NULL,
	symbol text NOT NULL,
	decision text NOT NULL,
	price NUMERIC NOT NULL,
	seconds5 NUMERIC NOT NULL,
	seconds10 NUMERIC NOT NULL,
	seconds15 NUMERIC NOT NULL,
	seconds30 NUMERIC NOT NULL,
	seconds45 NUMERIC NOT NULL,
	minute1 NUMERIC NOT NULL,
	minutes2 NUMERIC NOT NULL,
	minutes3 NUMERIC NOT NULL,
	minutes5 NUMERIC NOT NULL,
	minutes7 NUMERIC NOT NULL,
	minutes10 NUMERIC NOT NULL,
	minutes15 NUMERIC NOT NULL,
	minutes30 NUMERIC NOT NULL,
	hour1 NUMERIC NOT NULL,
	hours2 NUMERIC NOT NULL,
	hours3 NUMERIC NOT NULL,
	hours6 NUMERIC NOT NULL,
	hours12 NUMERIC NOT NULL,
	hours18 NUMERIC NOT NULL,
	day1 NUMERIC NOT NULL,
	PRIMARY KEY (id)
);

CREATE INDEX ix_d_ets ON decision USING BRIN (event_time, symbol);

CREATE OR REPLACE FUNCTION insert_decision
(
	p_symbol TEXT,
	p_decision TEXT,
	p_price NUMERIC,	
	p_seconds5 NUMERIC,
	p_seconds10 NUMERIC,
	p_seconds15 NUMERIC,
	p_seconds30 NUMERIC,
	p_seconds45 NUMERIC,
	p_minute1 NUMERIC,
	p_minutes2 NUMERIC,
	p_minutes3 NUMERIC,
	p_minutes5 NUMERIC,
	p_minutes7 NUMERIC,
	p_minutes10 NUMERIC,
	p_minutes15 NUMERIC,
	p_minutes30 NUMERIC,
	p_hour1 NUMERIC,
	p_hours2 NUMERIC,
	p_hours3 NUMERIC,
	p_hours6 NUMERIC,
	p_hours12 NUMERIC,
	p_hours18 NUMERIC,
	p_day1 NUMERIC
)
RETURNS void AS
$$
BEGIN

	INSERT INTO decision (symbol, decision, price, seconds5, seconds10, seconds15, seconds30, seconds45, minute1, minutes2, minutes3, minutes5, minutes7, minutes10, minutes15,
							minutes30, hour1, hours2, hours3, hours6, hours12, hours18, day1)
			VALUES (p_symbol, p_decision, p_price, p_seconds5, p_seconds10, p_seconds15, p_seconds30, p_seconds45, p_minute1, p_minutes2, p_minutes3, p_minutes5, p_minutes7,
					p_minutes10, p_minutes15, p_minutes30, p_hour1, p_hours2, p_hours3, p_hours6, p_hours12, p_hours18, p_day1);

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;



CREATE OR REPLACE FUNCTION stats_3s() 
RETURNS TABLE (
	r_symbol TEXT,
	r_databasis INT,
	r_slope_5s NUMERIC,
	r_slope_10s NUMERIC,
	r_slope_15s NUMERIC,
	r_slope_30s NUMERIC,
	r_movav_5s NUMERIC,
	r_movav_10s NUMERIC,
	r_movav_15s NUMERIC,
	r_movav_30s NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '5 seconds'))::NUMERIC, 8) AS slope_5s,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '10 seconds'))::NUMERIC, 8) AS slope_10s,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '15 seconds'))::NUMERIC, 8) AS slope_15s,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_30s,
		ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '5 seconds') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '5 seconds')), 8) AS movav_5s,
		ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '10 seconds') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '10 seconds')), 8) AS movav_10s,
		ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '15 seconds') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '15 seconds')), 8) AS movav_15s,
		ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_30s
	FROM binance_stream_tick
	WHERE event_time >= NOW() - INTERVAL '30 seconds'
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;



CREATE OR REPLACE FUNCTION stats_15s() 
RETURNS TABLE (
	r_symbol TEXT,
	r_databasis INT,
	r_slope_45s NUMERIC,
	r_slope_1m NUMERIC,
	r_slope_2m NUMERIC,
	r_slope_3m NUMERIC,
	r_movav_45s NUMERIC,
	r_movav_1m NUMERIC,
	r_movav_2m NUMERIC,
	r_movav_3m NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '45 seconds'))::NUMERIC, 8) AS slope_45s,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '1 minute'))::NUMERIC, 8) AS slope_1m,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '2 minutes'))::NUMERIC, 8) AS slope_2m,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_3m,
		ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '45 second') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '45 second')), 8) AS movav_45s,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '1 minute') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '1 minute')), 8) AS movav_1m,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '2 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '2 minutes')), 8) AS movav_2m,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_3m
	FROM binance_stream_tick
	WHERE event_time >= NOW() - INTERVAL '3 minutes'
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;


CREATE OR REPLACE FUNCTION stats_2m() 
RETURNS TABLE (
	r_symbol TEXT,
	r_databasis INT,
	r_slope_5m NUMERIC,
	r_slope_7m NUMERIC,
	r_slope_10m NUMERIC,
	r_slope_15m NUMERIC,
	r_movav_5m NUMERIC,
	r_movav_7m NUMERIC,
	r_movav_10m NUMERIC,
	r_movav_15m NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '5 minutes'))::NUMERIC, 8) AS slope_5m,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '7 minutes'))::NUMERIC, 8) AS slope_7m,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '10 minutes'))::NUMERIC, 8) AS slope_10,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_15m,
		ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '5 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '5 minutes')), 8) AS movav_5m,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '7 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '7 minutes')), 8) AS movav_7m,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '10 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '10 minutes')), 8) AS movav_10m,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_15m
	FROM binance_stream_tick
	WHERE event_time >= NOW() - INTERVAL '15 minutes'
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;



CREATE OR REPLACE FUNCTION stats_10m()
RETURNS TABLE (
	r_symbol TEXT,
	r_databasis INT,
	r_slope_30m NUMERIC,
	r_slope_1h NUMERIC,
	r_slope_2h NUMERIC,
	r_slope_3h NUMERIC,
	r_movav_30m NUMERIC,
	r_movav_1h NUMERIC,
	r_movav_2h NUMERIC,
	r_movav_3h NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '30 minutes'))::NUMERIC, 8) AS slope_30m,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '1 hour'))::NUMERIC, 8) AS slope_1h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '2 hours'))::NUMERIC, 8) AS slope_2h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_3h,
		ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '30 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '30 minutes')), 8) AS movav_30m,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '1 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '1 hours')), 8) AS movav_1h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '2 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '2 hours')), 8) AS movav_2h,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_3h
	FROM binance_stream_tick
	WHERE event_time >= NOW() - INTERVAL '3 hours'
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;



CREATE OR REPLACE FUNCTION stats_2h()
RETURNS TABLE (
	r_symbol TEXT,
	r_databasis INT,
	r_slope_6h NUMERIC,
	r_slope_12h NUMERIC,
	r_slope_18h NUMERIC,
	r_slope_1d NUMERIC,
	r_movav_6h NUMERIC,
	r_movav_12h NUMERIC,
	r_movav_18h NUMERIC,
	r_movav_1d NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '6 hours'))::NUMERIC, 8) AS slope_6h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '12 hours'))::NUMERIC, 8) AS slope_12h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '18 hours'))::NUMERIC, 8) AS slope_18h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_1d,
		ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '6 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '6 hours')), 8) AS movav_6h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '12 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '12 hours')), 8) AS movav_12h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE event_time >= NOW() - INTERVAL '18 hours') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '18 hours')), 8) AS movav_18h,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_1d
	FROM binance_stream_tick
	WHERE event_time >= NOW() - INTERVAL '1 day'
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;
