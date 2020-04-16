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
		WHERE event_time > NOW() - INTERVAL '3 seconds'
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



CREATE OR REPLACE FUNCTION public.stats_3s(
	)
    RETURNS TABLE(r_symbol text, r_databasis integer, r_slope_5s numeric, r_slope_10s numeric, r_slope_15s numeric, r_slope_30s numeric, r_movav_5s numeric, r_movav_10s numeric, r_movav_15s numeric, r_movav_30s numeric) 
    LANGUAGE 'plpgsql'

    COST 100
    VOLATILE STRICT 
    ROWS 1000
    
AS $BODY$
BEGIN
	RETURN QUERY SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '5 seconds'))::NUMERIC, 8) AS slope_5s,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '10 seconds'))::NUMERIC, 8) AS slope_10s,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '15 seconds'))::NUMERIC, 8) AS slope_15s,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_30s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE event_time >= NOW() - INTERVAL '5 seconds') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '5 seconds')), 8) AS movav_5s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE event_time >= NOW() - INTERVAL '10 seconds') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '10 seconds')), 8) AS movav_10s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE event_time >= NOW() - INTERVAL '15 seconds') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '15 seconds')), 8) AS movav_15s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) / COUNT(*)), 8) AS movav_30s
	FROM binance_book_tick
	WHERE event_time >= NOW() - INTERVAL '30 seconds'
	GROUP BY symbol;
END;
$BODY$;



CREATE OR REPLACE FUNCTION public.stats_15s(
	)
    RETURNS TABLE(r_symbol text, r_databasis integer, r_slope_45s numeric, r_slope_1m numeric, r_slope_2m numeric, r_slope_3m numeric, r_movav_45s numeric, r_movav_1m numeric, r_movav_2m numeric, r_movav_3m numeric) 
    LANGUAGE 'plpgsql'

    COST 100
    VOLATILE STRICT 
    ROWS 1000
    
AS $BODY$
BEGIN
	RETURN QUERY SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '45 seconds'))::NUMERIC, 8) AS slope_45s,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '1 minute'))::NUMERIC, 8) AS slope_1m,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '2 minutes'))::NUMERIC, 8) AS slope_2m,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_3m,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE event_time >= NOW() - INTERVAL '45 second') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '45 second')), 8) AS movav_45s,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE event_time >= NOW() - INTERVAL '1 minute') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '1 minute')), 8) AS movav_1m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE event_time >= NOW() - INTERVAL '2 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '2 minutes')), 8) AS movav_2m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) / COUNT(*)), 8) AS movav_3m
	FROM binance_book_tick
	WHERE event_time >= NOW() - INTERVAL '3 minutes'
	GROUP BY symbol;
END;
$BODY$;

ALTER FUNCTION public.stats_15s()
    OWNER TO trape;



CREATE OR REPLACE FUNCTION public.stats_2m(
	)
    RETURNS TABLE(r_symbol text, r_databasis integer, r_slope_5m numeric, r_slope_7m numeric, r_slope_10m numeric, r_slope_15m numeric, r_movav_5m numeric, r_movav_7m numeric, r_movav_10m numeric, r_movav_15m numeric) 
    LANGUAGE 'plpgsql'

    COST 100
    VOLATILE STRICT 
    ROWS 1000
    
AS $BODY$
BEGIN
	RETURN QUERY SELECT symbol, COUNT(*)::INT,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '5 minutes'))::NUMERIC, 8) AS slope_5m,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '7 minutes'))::NUMERIC, 8) AS slope_7m,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time >= NOW() - INTERVAL '10 minutes'))::NUMERIC, 8) AS slope_10,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM event_time)))::NUMERIC, 8) AS slope_15m,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE event_time >= NOW() - INTERVAL '5 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '5 minutes')), 8) AS movav_5m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE event_time >= NOW() - INTERVAL '7 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '7 minutes')), 8) AS movav_7m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE event_time >= NOW() - INTERVAL '10 minutes') 
			/ COUNT(*) FILTER (WHERE event_time >= NOW() - INTERVAL '10 minutes')), 8) AS movav_10m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) / COUNT(*)), 8) AS movav_15m
	FROM binance_book_tick
	WHERE event_time >= NOW() - INTERVAL '15 minutes'
	GROUP BY symbol;
END;
$BODY$;



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

--SELECT * FROM get_recommendation_history('ETHUSDT')

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

CREATE TABLE binance_stream_balance
(
	id BIGSERIAL NOT NULL,
	event_time TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	asset TEXT NOT NULL,
	free NUMERIC NOT NULL,
	locked NUMERIC NOT NULL,
	total NUMERIC NOT NULL,
	PRIMARY KEY (id)
);

CREATE INDEX ix_bsb_es ON binance_stream_balance USING BRIN (event_time, asset);

CREATE FUNCTION insert_binance_stream_balance (p_asset TEXT, p_free NUMERIC, p_locked NUMERIC, p_total NUMERIC)
RETURNS void AS
$$
BEGIN

	INSERT INTO binance_stream_balance (asset, free, locked, total)
			VALUES (p_asset, p_free, p_locked, p_total);

END;
$$
LANGUAGE plpgsql VOLATILE ;

DROP TABLE binance_stream_balance_update;
DROP FUNCTION insert_binance_stream_balance_update(timestamptz, text, text, numeric, timestamptz);
CREATE TABLE binance_stream_balance_update
(
	id BIGSERIAL NOT NULL,
	event_time TIMESTAMPTZ NOT NULL,
	event TEXT NOT NULL,
	asset TEXT NOT NULL,
	balance_delta NUMERIC NOT NULL,
	clear_time TIMESTAMPTZ NOT NULL,
	PRIMARY KEY (id)
);

CREATE INDEX ix_bsbu_eta ON binance_stream_balance_update USING BRIN (event_time, asset);


CREATE FUNCTION insert_binance_stream_balance_update (p_event_time TIMESTAMPTZ, p_event TEXT, p_asset TEXT, p_balance_delta NUMERIC, p_clear_time TIMESTAMPTZ)
RETURNS void AS
$$
BEGIN

	INSERT INTO binance_stream_balance_update (event_time, event, asset, balance_delta, clear_time)
			VALUES (p_event_time, p_event, p_asset, p_balance_delta, p_clear_time);

END;
$$
LANGUAGE plpgsql VOLATILE ;



DROP FUNCTION insert_binance_stream_order_list (TEXT, TIMESTAMPTZ, INT8, TEXT, TEXT, TEXT, TEXT);
DROP TABLE binance_stream_order_list;

CREATE TABLE binance_stream_order_list
(
	id BIGSERIAL NOT NULL,
	event_time TIMESTAMPTZ NOT NULL,
	event TEXT NOT NULL,
	symbol TEXT NOT NULL,
	transaction_time TIMESTAMPTZ NOT NULL,
	order_list_id INT8 NOT NULL,
	contingency_type TEXT NOT NULL,
	list_status_type TEXT NOT NULL,
	list_order_status TEXT NOT NULL,
	list_client_order_id TEXT NOT NULL,
	PRIMARY KEY (id)
);

CREATE INDEX ix_bsol_ets ON binance_stream_order_list USING BRIN (event_time, symbol);
CREATE INDEX ix_bsol_s ON binance_stream_order_list (symbol);
CREATE INDEX ix_bsol_coid ON binance_stream_order_list (list_client_order_id);

CREATE TABLE binance_stream_order_id
(
	id BIGSERIAL NOT NULL,
	symbol TEXT NOT NULL,
	order_id INT8 NOT NULL,
	client_order_id TEXT NOT NULL,
	PRIMARY KEY (id)
);

CREATE INDEX ix_bsoi_oid ON binance_stream_order_id (order_id);
CREATE INDEX ix_bsoi_coid ON binance_stream_order_id (client_order_id);

CREATE OR REPLACE FUNCTION insert_binance_stream_order_list
	(p_event_time TIMESTAMPTZ, p_event TEXT, p_symbol TEXT, p_transaction_time TIMESTAMPTZ, p_order_list_id INT8, p_contingency_type TEXT, p_list_status_type TEXT,
		p_list_order_status TEXT, p_list_client_order_id TEXT)
RETURNS VOID AS
$$
BEGIN
	INSERT INTO binance_stream_order_list (event_time, event, symbol, transaction_time, order_list_id, contingency_type, list_status_type, list_order_status, list_client_order_id)
		VALUES(p_event_time, p_eventp_symbol, p_transaction_time, p_order_list_id, p_contingency_type, p_list_status_type, p_list_order_status, p_list_client_order_id);
END;
$$
LANGUAGE plpgsql VOLATILE ;

CREATE OR REPLACE FUNCTION insert_binance_stream_order_id
	(p_symbol TEXT, p_order_id INT8, client_order_id TEXT)
RETURNS VOID AS
$$
BEGIN
	INSERT INTO binance_stream_order_id (symbol, order_id, client_order_id)
		VALUES(p_symbol, p_order_id, p_client_order_id);
END;
$$
LANGUAGE plpgsql VOLATILE ;







CREATE TABLE binance_stream_order_update
(
	id BIGSERIAL NOT NULL,
	event TEXT NOT NULL,
	event_time TIMESTAMPTZ NOT NULL,
	last_quote_transacted_quantity NUMERIC NOT NULL,
	quote_order_quantity NUMERIC NOT NULL,
	cumulative_quote_quantity NUMERIC NOT NULL,
	order_creation_time TIMESTAMPTZ NOT NULL,
	buyer_is_maker BOOLEAN NOT NULL,
	is_working BOOLEAN NOT NULL,
	trade_id INT8 NOT NULL,
	time TIMESTAMPTZ NOT NULL,
	commission_asset TEXT,
	commission NUMERIC NOT NULL,
	price_last_filled_trade NUMERIC NOT NULL,
	accumulated_quantity_of_filled_trades NUMERIC NOT NULL,
	quantity_of_last_filled_trade NUMERIC NOT NULL,
	order_id INT8 NOT NULL,
	reject_reason TEXT NOT NULL,
	status TEXT NOT NULL,
	execution_type TEXT NOT NULL,
	original_client_order_id TEXT,
	iceberg_quantity NUMERIC NOT NULL,
	stop_price NUMERIC NOT NULL,
	price NUMERIC NOT NULL,
	quantity NUMERIC NOT NULL,
	time_in_force TEXT NOT NULL,
	type TEXT NOT NULL,
	side TEXT NOT NULL,
	client_order_id TEXT NOT NULL,
	symbol TEXT NOT NULL,
	order_list_id INT8 NOT NULL,
	unused_i INT8 NOT NULL,
	PRIMARY KEY (id)
);

CREATE INDEX ix_bsou_ets ON binance_stream_order_update USING BRIN (event_time, symbol);
CREATE INDEX ix_bsou_ti ON binance_stream_order_update (trade_id);
CREATE INDEX ix_bsou_oi ON binance_stream_order_update (order_id);
CREATE INDEX ix_bsou_ocoi ON binance_stream_order_update (original_client_order_id);
CREATE INDEX ix_bsou_coi ON binance_stream_order_update (client_order_id);
CREATE INDEX ix_bsou_oli ON binance_stream_order_update (order_list_id);

CREATE OR REPLACE FUNCTION insert_binance_stream_order_update
(
	p_event TEXT,
	p_event_time TIMESTAMPTZ,
	p_last_quote_transacted_quantity NUMERIC,
	p_quote_order_quantity NUMERIC,
	p_cumulative_quote_quantity NUMERIC,
	p_order_creation_time TIMESTAMPTZ,
	p_buyer_is_maker BOOLEAN,
	p_is_working BOOLEAN,
	p_trade_id INT8,
	p_time TIMESTAMPTZ,
	p_commission_asset TEXT,
	p_commission NUMERIC,
	p_price_last_filled_trade NUMERIC,
	p_accumulated_quantity_of_filled_trades NUMERIC,
	p_quantity_of_last_filled_trade NUMERIC,
	p_order_id INT8,
	p_reject_reason TEXT,
	p_status TEXT,
	p_execution_type TEXT,
	p_original_client_order_id TEXT,
	p_iceberg_quantity NUMERIC,
	p_stop_price NUMERIC,
	p_price NUMERIC,
	p_quantity NUMERIC,
	p_time_in_force TEXT,
	p_type TEXT,
	p_side TEXT,
	p_client_order_id TEXT,
	p_symbol TEXT,
	p_order_list_id INT8,
	p_unused_i INT8
)
RETURNS VOID AS
$$
BEGIN
	INSERT INTO binance_stream_order_update
	(
		event,
		event_time,
		last_quote_transacted_quantity,
		quote_order_quantity,
		cumulative_quote_quantity,
		order_creation_time,
		buyer_is_maker,
		is_working,
		trade_id,
		time,
		commission_asset,
		commission,
		price_last_filled_trade,
		accumulated_quantity_of_filled_trades,
		quantity_of_last_filled_trade,
		order_id,
		reject_reason,
		status,
		execution_type,
		original_client_order_id,
		iceberg_quantity,
		stop_price,
		price,
		quantity,
		time_in_force,
		type,
		side,
		client_order_id,
		symbol,
		order_list_id,
		unused_i
	)
	VALUES
	(
		p_event,
		p_event_time,
		p_last_quote_transacted_quantity,
		p_quote_order_quantity,
		p_cumulative_quote_quantity,
		p_order_creation_time,
		p_buyer_is_maker,
		p_is_working,
		p_trade_id,
		p_time,
		p_commission_asset,
		p_commission,
		p_price_last_filled_trade,
		p_accumulated_quantity_of_filled_trades,
		p_quantity_of_last_filled_trade,
		p_order_id,
		p_reject_reason,
		p_status,
		p_execution_type,
		p_original_client_order_id,
		p_iceberg_quantity,
		p_stop_price,
		p_price,
		p_quantity,
		p_time_in_force,
		p_type,
		p_side,
		p_client_order_id,
		p_symbol,
		p_order_list_id,
		p_unused_i
	);

END;
$$
LANGUAGE plpgsql VOLATILE ;


CREATE TABLE "order"
(
	id BIGSERIAL NOT NULL,
	event_time TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	symbol TEXT NOT NULL,
	side TEXT NOT NULL,
	type TEXT NOT NULL,
	quote_order_quantity NUMERIC NOT NULL,
	price NUMERIC NOT NULL,
	new_client_order_id TEXT NOT NULL,
	order_response_type TEXT NOT NULL,
	time_in_force TEXT NOT NULL,
	PRIMARY KEY(id)
);

CREATE INDEX ix_o_ets ON "order" USING BRIN (event_time, symbol);
CREATE INDEX ix_o_ncoi ON "order" (new_client_order_id);

CREATE OR REPLACE FUNCTION insert_order
(
	p_symbol TEXT,
	p_side TEXT,
	p_type TEXT,
	p_quote_order_quantity NUMERIC,
	p_price NUMERIC,
	p_new_client_order_id TEXT,
	p_order_response_type TEXT,
	p_time_in_force TEXT
)
RETURNS VOID AS
$$
BEGIN
	INSERT INTO "order"
	(
		symbol,
		side,
		type,
		quote_order_quantity,
		price,
		new_client_order_id,
		order_response_type,
		time_in_force
	)
	VALUES
	(
		p_symbol,
		p_side,
		p_type,
		p_quote_order_quantity,
		p_price,
		p_new_client_order_id,
		p_order_response_type,
		p_time_in_force
	);

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;


CREATE OR REPLACE FUNCTION select_last_order (p_symbol TEXT)
RETURNS SETOF "order"
AS
$$
BEGIN
	RETURN QUERY SELECT * FROM "order"
			WHERE symbol = p_symbol
			ORDER BY id DESC LIMIT 1;
END;
$$
LANGUAGE plpgsql STRICT;


CREATE TABLE binance_placed_order
(
	id BIGSERIAL NOT NULL,
	margin_buy_borrow_asset TEXT,
	margin_buy_borrow_amount NUMERIC,
	stop_price NUMERIC,
	side TEXT NOT NULL,
	type TEXT NOT NULL,
	time_in_force TEXT NOT NULL,
	status TEXT NOT NULL,
	original_quote_order_quantity NUMERIC NOT NULL,
	cumulative_quote_quantity NUMERIC NOT NULL,
	executed_quantity NUMERIC NOT NULL,
	original_quantity NUMERIC NOT NULL,
	price NUMERIC NOT NULL,
	transaction_time TIMESTAMPTZ NOT NULL,
	original_client_order_id TEXT NOT NULL,
	client_order_id TEXT NOT NULL,
	order_id INT8 NOT NULL,
	symbol TEXT NOT NULL,
	order_list_id INT8,
	PRIMARY KEY (id)
);
CREATE INDEX ix_bpo_ocoi ON binance_placed_order (original_client_order_id);
CREATE INDEX ix_bpo_cli ON binance_placed_order (client_order_id);
CREATE INDEX ix_bpo_oi ON binance_placed_order (order_id);
CREATE OR REPLACE FUNCTION insert_binance_placed_order
(
	p_margin_buy_borrow_asset TEXT,
	p_margin_buy_borrow_amount NUMERIC,
	p_stop_price NUMERIC,
	p_side TEXT,
	p_type TEXT,
	p_time_in_force TEXT,
	p_status TEXT,
	p_original_quote_order_quantity NUMERIC,
	p_cumulative_quote_quantity NUMERIC,
	p_executed_quantity NUMERIC,
	p_original_quantity NUMERIC,
	p_price NUMERIC,
	p_transaction_time TIMESTAMPTZ,
	p_original_client_order_id TEXT,
	p_client_order_id TEXT,
	p_order_id INT8,
	p_symbol TEXT,
	p_order_list_id INT8
)
RETURNS INT8 AS
$$
	DECLARE r_id INT8;
BEGIN
	INSERT INTO binance_placed_order
	(
		margin_buy_borrow_asset,
		margin_buy_borrow_amount,
		stop_price,
		side,
		type,
		time_in_force,
		status,
		original_quote_order_quantity,
		cumulative_quote_quantity,
		executed_quantity,
		original_quantity,
		price,
		transaction_time,
		original_client_order_id,
		client_order_id,
		order_id,
		symbol,
		order_list_id
	)
	VALUES
	(
		p_margin_buy_borrow_asset,
		p_margin_buy_borrow_amount,
		p_stop_price,
		p_side,
		p_type,
		p_time_in_force,
		p_status,
		p_original_quote_order_quantity,
		p_cumulative_quote_quantity,
		p_executed_quantity,
		p_original_quantity,
		p_price,
		p_transaction_time,
		p_original_client_order_id,
		p_client_order_id,
		p_order_id,
		p_symbol,
		p_order_list_id
	) RETURNING id INTO r_id;
	
	RETURN r_id;
END;
$$
LANGUAGE plpgsql VOLATILE;

CREATE TABLE binance_order_trade
(
	id BIGSERIAL NOT NULL,
	binance_placed_order_id INT8 NOT NULL,
	trade_id INT8 NOT NULL,
	price NUMERIC NOT NULL,
	quantity NUMERIC NOT NULL,
	commission NUMERIC NOT NULL,
	commission_asset TEXT NOT NULL,
	consumed NUMERIC NOT NULL DEFAULT 0,
	consumed_price NUMERIC NOT NULL DEFAULT 0,
	PRIMARY KEY (id)
);
CREATE INDEX ix_bot_bpoi ON binance_order_trade (binance_placed_order_id);

CREATE OR REPLACE FUNCTION insert_binance_order_trade
(
	p_binance_placed_order_id INT8,
	p_trade_id INT8,
	p_price NUMERIC,
	p_quantity NUMERIC,
	p_commission NUMERIC,
	p_commission_asset TEXT
)
RETURNS VOID AS
$$
BEGIN
	INSERT INTO binance_order_trade
	(
		binance_placed_order_id,
		trade_id,
		price,
		quantity,
		commission,
		commission_asset
	)
	VALUES
	(
		p_binance_placed_order_id,
		p_trade_id,
		p_price,
		p_quantity,
		p_commission,
		p_commission_asset
	);
END;
$$
LANGUAGE plpgsql VOLATILE;





CREATE OR REPLACE FUNCTION select_last_orders(p_symbol TEXT)
RETURNS TABLE
(
	r_binance_placed_order_id BIGINT,
	r_transaction_time TIMESTAMPTZ,	
	r_symbol TEXT,
	r_side TEXT,
	r_price NUMERIC,
	r_quantity NUMERIC,
	r_consumed NUMERIC,
	r_consumed_price NUMERIC
)
AS
$$
BEGIN
	RETURN QUERY SELECT binance_placed_order_id, transaction_time, symbol, bpo.side, bot.price, quantity, consumed, consumed_price FROM binance_order_trade bot
		INNER JOIN binance_placed_order bpo ON bpo.id = bot.binance_placed_order_id
		WHERE bpo.symbol = p_symbol AND consumed < quantity;
END;
$$
LANGUAGE plpgsql VOLATILE STRICT;





DROP TRIGGER tr_update_traded_quantity ON binance_order_trade;
DROP FUNCTION update_traded_quantity;
CREATE OR REPLACE FUNCTION update_traded_quantity ()
RETURNS TRIGGER AS
$$
	DECLARE i_trade binance_order_trade%ROWTYPE;
			i_side TEXT;
			i_quantity NUMERIC;
			i_free_quantity NUMERIC;
			i_consume_quantity NUMERIC;
			i_consume_price NUMERIC;
			i_new_consumed_total NUMERIC;	
			i_part_consumed NUMERIC;
			i_part_consume NUMERIC;
			i_new_consumed_price NUMERIC;
			i_price NUMERIC;
BEGIN
	-- Check if new lines come from Buy or Sell
	SELECT side INTO i_side FROM binance_placed_order WHERE id = NEW.binance_placed_order_id;
	-- Modify the other one, as this has to be consumed
	
	i_quantity := NEW.quantity;
	i_price := NEW.price;
	
	IF (i_side = 'Sell') THEN
		
		FOR i_trade IN SELECT bot.* FROM binance_placed_order bpo
						INNER JOIN binance_order_trade bot ON bot.binance_placed_order_id = bpo.id
						WHERE bpo.side = 'Buy'
							AND bot.consumed < bot.quantity
							AND bot.price <= NEW.price
						ORDER BY bot.price DESC FOR UPDATE OF bpo
		LOOP
			IF (i_quantity > 0) THEN
				i_free_quantity := i_trade.quantity - i_trade.consumed;

				IF (i_quantity > i_free_quantity) THEN
					i_quantity := i_quantity - i_free_quantity;
					i_consume_quantity := i_free_quantity;
				ELSE
					i_consume_quantity := i_quantity;
					i_quantity := 0;
				END IF;
				
				i_new_consumed_total := i_trade.consumed + i_consume_quantity;
				i_part_consumed := i_trade.consumed / i_new_consumed_total;
				i_part_consume := i_consume_quantity / i_new_consumed_total;				
				i_new_consumed_price := i_part_consumed * i_trade.price + i_part_consume * i_price;
				
				UPDATE binance_order_trade
					SET consumed = i_new_consumed_total, consumed_price = i_new_consumed_price
					WHERE binance_order_trade.id = i_trade.id;
				
			END IF;
		END LOOP;
	END IF;

	RETURN NEW;
END;
$$
LANGUAGE plpgsql VOLATILE STRICT;


CREATE TRIGGER tr_update_traded_quantity
    AFTER INSERT ON binance_order_trade
    FOR EACH ROW
    EXECUTE PROCEDURE update_traded_quantity();




CREATE OR REPLACE FUNCTION public.select_asset_status(
	)
    RETURNS TABLE(r_symbol text, r_bought numeric, r_sold numeric, r_remaining numeric) 
    LANGUAGE 'plpgsql'

    COST 100
    VOLATILE STRICT 
    ROWS 1000
    
AS $BODY$
BEGIN
	RETURN QUERY SELECT buy.symbol, buy.quantity as bought, buy.consumed as sold, (buy.quantity - buy.consumed - buy.commission) as remains FROM
	(
		SELECT bpo.symbol, bpo.side, SUM(quantity) quantity, SUM(consumed) consumed, SUM(quantity-consumed) remaining,
				SUM(CASE WHEN commission_asset = REPLACE(bpo.symbol, 'USDT', '') THEN commission ELSE 0 END) AS commission FROM binance_order_trade bot
			LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
			WHERE side = 'Buy'
			GROUP BY bpo.symbol, bpo.side
			ORDER BY bpo.symbol
	) buy;
END;
$BODY$;

ALTER FUNCTION select_asset_status()
    OWNER TO trape;

CREATE OR REPLACE FUNCTION fix_symbol_quantity (p_symbol TEXT, p_quantity NUMERIC, p_price NUMERIC)
RETURNS VOID AS
$$
	DECLARE i_id BIGINT;
BEGIN
	UPDATE binance_order_trade SET consumed = quantity WHERE binance_placed_order_id IN 
		(SELECT id FROM binance_placed_order WHERE symbol = p_symbol);
		
	INSERT INTO binance_placed_order (side, type, time_in_force, status, original_quote_order_quantity, 
									 cumulative_quote_quantity, executed_quantity, original_quantity,
									 price, order_id, symbol, transaction_time, original_client_order_id,
									 client_order_id)
						 VALUES ('Buy', 'Market', 'GoodTillCancel', 'Filled', 0, 0, 0, 0, 0,
									0, p_symbol, now(), 0, 'fix') RETURNING id INTO i_id;
									
	INSERT INTO binance_order_trade (binance_placed_order_id, trade_id, price, quantity, commission, commission_asset)
		VALUES (i_id, -1, p_price, p_quantity, 0, 'BNB');
END;
$$
LANGUAGE plpgsql VOLATILE STRICT;



CREATE OR REPLACE FUNCTION current_statement ()
RETURNS TABLE (
	date DATE,
	symbol TEXT,
	buy NUMERIC,
	sell NUMERIC,
	profit NUMERIC)
	AS
$$
BEGIN
	RETURN QUERY
	SELECT transaction_time, a.symbol, ROUND(a.buy, 8), ROUND(a.sell, 8), ROUND(a.sell-a.buy, 8) AS profit FROM (
		SELECT	transaction_time::DATE,
				bop.symbol,
				SUM(bot.price*consumed) as buy,
				SUM(consumed_price*consumed) as sell
				FROM binance_order_trade bot
				INNER JOIN binance_placed_order bop ON bop.id = bot.binance_placed_order_id
		WHERE side = 'Buy'
		GROUP BY transaction_time::DATE, bop.symbol ) a
	ORDER BY transaction_time::DATE DESC, symbol ASC;
END;
$$
LANGUAGE plpgsql STRICT;