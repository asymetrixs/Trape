--select * from binance_stream_kline_data order by id
-- select * from binance_stream_tick
--update binance_stream_kline_data set final = true where id = 74
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
	) ON CONFLICT (open_time, interval, symbol) DO UPDATE SET
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