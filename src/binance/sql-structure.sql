CREATE TABLE candlestick
(
	open_time timestamptz NOT NULL,
	close_time timestamptz NOT NULL,
	symbol text NOT NULL,
	interval text NOT NULL,
	open decimal NOT NULL,
	close decimal NOT NULL,
	high decimal NOT NULL,
	low decimal NOT NULL,
	number_of_trades int4 NOT NULL,
	quote_assed_volume decimal NOT NULL,
	taker_buy_base_assed_volume decimal NOT NULL,
	taker_buy_quote_assed_volume decimal NOT NULL,
	quote_asset_volume decimal NOT NULL,
	volume decimal NOT NULL
);

CREATE UNIQUE INDEX idx_cs_soti ON candlestick (symbol, open_time, interval);

DROP FUNCTION insert_candlestick
CREATE OR REPLACE FUNCTION insert_candlestick (
p_open_time timestamptz,
p_close_time timestamptz,
p_symbol text,
p_interval text,
p_open numeric,
p_close numeric,
p_high numeric,
p_low numeric,
p_number_of_trades int4,
p_quote_assed_volume numeric,
p_taker_buy_base_assed_volume numeric,
p_taker_buy_quote_assed_volume numeric,
p_quote_asset_volume numeric,
p_volume numeric
)
	RETURNS void AS
$$
BEGIN

	INSERT INTO candlestick (
		open_time,
		close_time,
		symbol,
		interval,
		open,
		close,
		high,
		low,
		number_of_trades,
		quote_assed_volume,
		taker_buy_base_assed_volume,
		taker_buy_quote_assed_volume,
		quote_asset_volume,
		volume
	) VALUES (
		p_open_time,
		p_close_time,
		p_symbol,
		p_interval,
		p_open,
		p_close,
		p_high,
		p_low,
		p_number_of_trades,
		p_quote_assed_volume,
		p_taker_buy_base_assed_volume,
		p_taker_buy_quote_assed_volume,
		p_quote_asset_volume,
		p_volume	
	) ON CONFLICT DO NOTHING;

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;


CREATE TABLE price
(
	datetime timestamptz NOT NULL,
	symbol text NOT NULL,
	price decimal NOT NULL
);

CREATE UNIQUE INDEX idx_p_rt ON price (symbol, datetime);


CREATE OR REPLACE FUNCTION insert_price (p_datetime TIMESTAMPTZ, p_symbol TEXT, p_price NUMERIC)
	RETURNS void AS
$$
BEGIN

	INSERT INTO price (datetime, symbol, price) VALUES (p_datetime, p_symbol, p_price)  ON CONFLICT DO NOTHING;

END;
$$
LANGUAGE plpgsql VOLATILE STRICT;

CREATE UNIQUE INDEX idx_p_ds ON price (datetime, symbol);
       