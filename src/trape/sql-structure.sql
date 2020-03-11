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


DROP FUNCTION trends_3sec();
CREATE OR REPLACE FUNCTION trends_3sec() 
RETURNS TABLE (
	r_symbol TEXT,
	r_databasis INT,
	r_seconds5 NUMERIC,
	r_seconds10 NUMERIC,
	r_seconds15 NUMERIC,
	r_seconds30 NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol, (COUNT(*) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '30 seconds' AND NOW()))::INT,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '5 seconds' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '10 seconds' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '15 seconds' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '30 seconds' AND NOW()))::NUMERIC
	FROM binance_stream_tick
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;


DROP FUNCTION trends_15sec();
CREATE OR REPLACE FUNCTION trends_15sec() 
RETURNS TABLE (
	r_symbol TEXT,
	r_databasis INT,
	r_seconds45 NUMERIC,
	r_minute1 NUMERIC,
	r_minutes2 NUMERIC,
	r_minutes3 NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol, (COUNT(*) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '3 minutes' AND NOW()))::INT,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '45 seconds' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '1 minute' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '2 minutes' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '3 minutes' AND NOW()))::NUMERIC
	FROM binance_stream_tick
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;



DROP FUNCTION trends_2min();
CREATE OR REPLACE FUNCTION trends_2min() 
RETURNS TABLE (
	r_symbol TEXT,
	r_databasis INT,
	r_minutes5 NUMERIC,
	r_minutes7 NUMERIC,
	r_minutes10 NUMERIC,
	r_minutes15 NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol, (COUNT(*) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '15 minutes' AND NOW()))::INT,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '5 minutes' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '7 minutes' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '10 minutes' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '15 minutes' AND NOW()))::NUMERIC
	FROM binance_stream_tick
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;



DROP FUNCTION trends_10min();
CREATE OR REPLACE FUNCTION trends_10min()
RETURNS TABLE (
	r_symbol TEXT,
	r_databasis INT,
	r_minutes30 NUMERIC,
	r_hour1 NUMERIC,
	r_hours2 NUMERIC,
	r_hours3 NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol, (COUNT(*) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '3 hours' AND NOW()))::INT,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '30 minutes' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '1 hour' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '2 hours' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '3 hours' AND NOW()))::NUMERIC
	FROM binance_stream_tick
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;



DROP FUNCTION  trends_2hours();
CREATE OR REPLACE FUNCTION trends_2hours()
RETURNS TABLE (
	r_symbol TEXT,
	r_databasis INT,
	r_hours6 NUMERIC,
	r_hours12 NUMERIC,
	r_hours18 NUMERIC,
	r_day1 NUMERIC
) AS
$$
BEGIN
	RETURN QUERY SELECT symbol, (COUNT(*) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '1 day' AND NOW()))::INT,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '6 hours' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '12 hours' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '18 hours' AND NOW()))::NUMERIC,
		(REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM event_time)) FILTER (WHERE event_time BETWEEN NOW() - INTERVAL '1 day' AND NOW()))::NUMERIC
	FROM binance_stream_tick
	GROUP BY symbol;
END;
$$
LANGUAGE plpgsql STRICT;


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

CREATE TABLE decision
(
	id bigserial NOT NULL,
	event_time TIMESTAMPTZ DEFAULT NOW() NOT NULL,
	symbol text NOT NULL,
	decision text NOT NULL,
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

	INSERT INTO decision (symbol, decision, seconds5, seconds10, seconds15, seconds30, seconds45, minute1, minutes2, minutes3, minutes5, minutes7, minutes10, minutes15,
							minutes30, hour1, hours2, hours3, hours6, hours12, hours18, day1)
			VALUES (p_symbol, p_decision, p_seconds5, p_seconds10, p_seconds15, p_seconds30, p_seconds45, p_minute1, p_minutes2, p_minutes3, p_minutes5, p_minutes7,
					p_minutes10, p_minutes15, p_minutes30, p_hour1, p_hours2, p_hours3, p_hours6, p_hours12, p_hours18, p_day1);

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;