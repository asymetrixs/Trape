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




CREATE TABLE recommendation
(
	id bigserial NOT NULL,
	event_time TIMESTAMPTZ DEFAULT NOW() NOT NULL,
	symbol text NOT NULL,
	decision text NOT NULL,
	price NUMERIC NOT NULL,
	slope5s NUMERIC NOT NULL,
	movav5s NUMERIC NOT NULL,
	slope10s NUMERIC NOT NULL,
	movav10s NUMERIC NOT NULL,
	slope15s NUMERIC NOT NULL,
	movav15s NUMERIC NOT NULL,
	slope30s NUMERIC NOT NULL,
	movav30s NUMERIC NOT NULL,
	slope45s NUMERIC NOT NULL,
	movav45s NUMERIC NOT NULL,
	slope1m NUMERIC NOT NULL,
	movav1m NUMERIC NOT NULL,
	slope2m NUMERIC NOT NULL,
	movav2m NUMERIC NOT NULL,
	slope3m NUMERIC NOT NULL,
	movav3m NUMERIC NOT NULL,
	slope5m NUMERIC NOT NULL,
	movav5m NUMERIC NOT NULL,
	slope7m NUMERIC NOT NULL,
	movav7m NUMERIC NOT NULL,
	slope10m NUMERIC NOT NULL,
	movav10m NUMERIC NOT NULL,
	slope15m NUMERIC NOT NULL,
	movav15m NUMERIC NOT NULL,
	slope30m NUMERIC NOT NULL,
	movav30m NUMERIC NOT NULL,
	slope1h NUMERIC NOT NULL,
	movav1h NUMERIC NOT NULL,
	slope2h NUMERIC NOT NULL,
	movav2h NUMERIC NOT NULL,
	slope3h NUMERIC NOT NULL,
	movav3h NUMERIC NOT NULL,
	slope6h NUMERIC NOT NULL,
	movav6h NUMERIC NOT NULL,
	slope12h NUMERIC NOT NULL,
	movav12h NUMERIC NOT NULL,
	slope18h NUMERIC NOT NULL,
	movav18h NUMERIC NOT NULL,
	slope1d NUMERIC NOT NULL,
	movav1d NUMERIC NOT NULL,
	PRIMARY KEY (id)
);

CREATE INDEX ix_r_ets ON recommendation USING BRIN (event_time, symbol);


CREATE OR REPLACE FUNCTION insert_recommendation
(
	p_symbol TEXT,
	p_decision TEXT,
	p_price NUMERIC,
	p_slope5s NUMERIC,
	p_movav5s NUMERIC,
	p_slope10s NUMERIC,
	p_movav10s NUMERIC,
	p_slope15s NUMERIC,
	p_movav15s NUMERIC,
	p_slope30s NUMERIC,
	p_movav30s NUMERIC,
	p_slope45s NUMERIC,
	p_movav45s NUMERIC,
	p_slope1m NUMERIC,
	p_movav1m NUMERIC,
	p_slope2m NUMERIC,
	p_movav2m NUMERIC,
	p_slope3m NUMERIC,
	p_movav3m NUMERIC,
	p_slope5m NUMERIC,
	p_movav5m NUMERIC,
	p_slope7m NUMERIC,
	p_movav7m NUMERIC,
	p_slope10m NUMERIC,
	p_movav10m NUMERIC,
	p_slope15m NUMERIC,
	p_movav15m NUMERIC,
	p_slope30m NUMERIC,
	p_movav30m NUMERIC,
	p_slope1h NUMERIC,
	p_movav1h NUMERIC,
	p_slope2h NUMERIC,
	p_movav2h NUMERIC,
	p_slope3h NUMERIC,
	p_movav3h NUMERIC,
	p_slope6h NUMERIC,
	p_movav6h NUMERIC,
	p_slope12h NUMERIC,
	p_movav12h NUMERIC,
	p_slope18h NUMERIC,
	p_movav18h NUMERIC,
	p_slope1d NUMERIC,
	p_movav1d NUMERIC
)
RETURNS void AS
$$
BEGIN

	INSERT INTO recommendation 
					(symbol, decision, price, slope5s, movav5s, slope10s, movav10s, slope15s, movav15s, slope30s, movav30s, slope45s, movav45s,
					slope1m, movav1m, slope2m, movav2m, slope3m, movav3m, slope5m, movav5m, slope7m, movav7m, slope10m, movav10m,
					slope15m, movav15m, slope30m, movav30m, slope1h, movav1h, slope2h, movav2h, slope3h, movav3h,
					slope6h, movav6h, slope12h, movav12h, slope18h, movav18h, slope1d, movav1d)
			VALUES (p_symbol, p_decision, p_price, p_slope5s, p_movav5s, p_slope10s, p_movav10s, p_slope15s, p_movav15s, p_slope30s, p_movav30s, p_slope45s, p_movav45s,
					p_slope1m, p_movav1m, p_slope2m, p_movav2m, p_slope3m, p_movav3m, p_slope5m, p_movav5m, p_slope7m, p_movav7m, p_slope10m, p_movav10m,
					p_slope15m, p_movav15m, p_slope30m, p_movav30m, p_slope1h, p_movav1h, p_slope2h, p_movav2h, p_slope3h, p_movav3h,
					p_slope6h, p_movav6h, p_slope12h, p_movav12h, p_slope18h, p_movav18h, p_slope1d, p_movav1d);

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

select * From stats_2m()
select * From stats_10m()

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

--SELECT * FROM get_recommendation_history('BTCUSDT')

CREATE OR REPLACE FUNCTION get_recommendation_history(p_symbol TEXT) RETURNS SETOF recommendation AS
$BODY$
DECLARE
    r recommendation%rowtype;
	action TEXT;
	last_action TEXT;
BEGIN
	--CREATE TEMPORARY TABLE recommendation_change ON COMMIT DROP AS SELECT * FROM recommendation ORDER BY id DESC LIMIT 1;
	
    FOR r IN SELECT * FROM recommendation WHERE symbol = p_symbol ORDER BY id DESC
    LOOP
		action := SUBSTRING(r.decision FROM 0 FOR 4);
		
		IF (action != last_action) THEN
        	RETURN NEXT r;
		END IF;
		
		last_action := action;
    END LOOP;
    RETURN;
END
$BODY$
LANGUAGE plpgsql;
