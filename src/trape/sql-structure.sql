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



--SELECT * FROM binance_book_tick WHERE symbol = 'BTCUSDT' order by update_id desc limit 600


CREATE TABLE trends
(
	id bigserial not null,
	created timestamptz not null default NOW(),
	symbol text NOT NULL,
	"5seconds" numeric not null,
	"10seconds" numeric not null,
	"15seconds" numeric not null,
	"30seconds" numeric not null,
	"45seconds" numeric not null,
	"1minute" numeric not null,
	"2minutes" numeric not null,
	"3minutes" numeric not null,
	"5minutes" numeric not null,
	"7minutes" numeric not null,
	"10minutes" numeric not null,
	"15minutes" numeric not null,
	"30minutes" numeric not null,
	"1hour" numeric not null,
	"2hours" numeric not null,
	"3hours" numeric not null,
	"6hours" numeric not null,
	"12hours" numeric not null,
	"18hours" numeric not null,
	"1day" numeric not null,
	"2days" numeric not null,
	PRIMARY KEY(id)
);
CREATE UNIQUE INDEX uq_trends_cs ON trends (created, symbol);


CREATE OR REPLACE FUNCTION collect_trends ()
	RETURNS void AS
$$
BEGIN

	INSERT INTO trends ("5seconds", "10seconds", "15seconds", "30seconds", "45seconds",
						"1minute", "2minutes", "3minutes","5minutes","7minutes","10minutes","15minutes",
						"30minutes","1hour","2hours","3hours","6hours","12hours","18hours","1day","2days")
	SELECT "5secs"."5secs", "10secs"."10secs", "15secs"."15secs", "30secs"."30secs", "45secs"."45secs",
		"1min"."1min", "2mins"."2mins", "3mins"."3mins", "5mins"."5mins", "7mins"."7mins", "10mins"."10mins", "15mins"."15mins", "30mins"."30mins",
		"1hour"."1hour", "2hours"."2hours", "3hours"."3hours", "6hours"."6hours", "12hours"."12hours", "18hours"."18hours", 
		"1day"."1day", "2days"."2days" FROM
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "5secs" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '5 seconds' AND NOW() AND symbol = 'BTCUSDT') "5secs",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "10secs" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '10 seconds' AND NOW() AND symbol = 'BTCUSDT') "10secs",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "15secs" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '15 seconds' AND NOW() AND symbol = 'BTCUSDT') "15secs",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "30secs" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '30 seconds' AND NOW() AND symbol = 'BTCUSDT') "30secs",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "45secs" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '45 seconds' AND NOW() AND symbol = 'BTCUSDT') "45secs",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "1min" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '1 minute' AND NOW() AND symbol = 'BTCUSDT') "1min",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "2mins" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '2 minutes' AND NOW() AND symbol = 'BTCUSDT') "2mins",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "3mins" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '3minutes' AND NOW() AND symbol = 'BTCUSDT') AS "3mins",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "5mins" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '5 minutes' AND NOW() AND symbol = 'BTCUSDT') AS "5mins",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "7mins" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '7 minutes' AND NOW() AND symbol = 'BTCUSDT') AS "7mins",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "10mins" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '10 minutes' AND NOW() AND symbol = 'BTCUSDT') AS "10mins",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "15mins" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '15 minutes' AND NOW() AND symbol = 'BTCUSDT') AS "15mins",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "30mins" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '30 minutes' AND NOW() AND symbol = 'BTCUSDT') AS "30mins",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "1hour" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '1 hour' AND NOW() AND symbol = 'BTCUSDT') AS "1hour",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "2hours" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '2 hours' AND NOW() AND symbol = 'BTCUSDT') AS "2hours",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "3hours" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '3 hours' AND NOW() AND symbol = 'BTCUSDT') AS "3hours",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "6hours" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '6 hours' AND NOW() AND symbol = 'BTCUSDT') AS "6hours",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "12hours" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '12 hours' AND NOW() AND symbol = 'BTCUSDT') AS "12hours",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "18hours" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '18 hours' AND NOW() AND symbol = 'BTCUSDT') AS "18hours",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "1day" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '1 days' AND NOW() AND symbol = 'BTCUSDT') AS "1day",
	(SELECT regr_slope(best_ask_price, EXTRACT(EPOCH FROM event_time)) AS "2days" FROM binance_book_tick
					WHERE event_time BETWEEN NOW() - INTERVAL '2 days' AND NOW() AND symbol = 'BTCUSDT') AS "2days";

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;