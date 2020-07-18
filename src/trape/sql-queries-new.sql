
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
		ROUND(((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '5 seconds'))*5)::NUMERIC, 8) AS slope_5s,
		ROUND(((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '10 seconds'))*10)::NUMERIC, 8) AS slope_10s,
		ROUND(((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '15 seconds'))*15)::NUMERIC, 8) AS slope_15s,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on))*30)::NUMERIC, 8) AS slope_30s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '5 seconds') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '5 seconds')), 8) AS movav_5s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '10 seconds') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '10 seconds')), 8) AS movav_10s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '15 seconds') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '15 seconds')), 8) AS movav_15s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) / COUNT(*)), 8) AS movav_30s
	FROM book_ticks
	WHERE created_on >= NOW() - INTERVAL '30 seconds'
	GROUP BY symbol
	HAVING COUNT(*) > 25;
	-- at least a value per second
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
		ROUND(((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '45 seconds'))*45)::NUMERIC, 8) AS slope_45s,
		ROUND(((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '1 minute'))*60)::NUMERIC, 8) AS slope_1m,
		ROUND(((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '2 minutes'))*60*2)::NUMERIC, 8) AS slope_2m,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on))*60*3)::NUMERIC, 8) AS slope_3m,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '45 second') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '45 second')), 8) AS movav_45s,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '1 minute') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '1 minute')), 8) AS movav_1m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '2 minutes') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '2 minutes')), 8) AS movav_2m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) / COUNT(*)), 8) AS movav_3m
	FROM book_ticks
	WHERE created_on >= NOW() - INTERVAL '3 minutes'
	GROUP BY symbol
	HAVING COUNT(*) > 170;
	-- at least a value per second
END;
$BODY$;




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
		ROUND(((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '5 minutes'))*60*5)::NUMERIC, 8) AS slope_5m,
		ROUND(((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '7 minutes'))*60*7)::NUMERIC, 8) AS slope_7m,
		ROUND(((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '10 minutes'))*60*10)::NUMERIC, 8) AS slope_10,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on))*60*15)::NUMERIC, 8) AS slope_15m,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '5 minutes') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '5 minutes')), 8) AS movav_5m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '7 minutes') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '7 minutes')), 8) AS movav_7m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '10 minutes') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '10 minutes')), 8) AS movav_10m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) / COUNT(*)), 8) AS movav_15m
	FROM book_ticks
	WHERE created_on >= NOW() - INTERVAL '15 minutes'
	GROUP BY symbol
	HAVING COUNT(*) > 860;
	-- at least a value per second
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
		ROUND(((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_close_time)) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '30 minutes'))*60*30)::NUMERIC, 8) AS slope_30m,
		ROUND(((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_close_time)) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '1 hour'))*60*60)::NUMERIC, 8) AS slope_1h,
		ROUND(((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_close_time)) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '2 hours'))*60*60*2)::NUMERIC, 8) AS slope_2h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_close_time))*60*60*3)::NUMERIC, 8) AS slope_3h,
		ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '30 minutes') 
			/ COUNT(*) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '30 minutes')), 8) AS movav_30m,
			ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '1 hours') 
			/ COUNT(*) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '1 hours')), 8) AS movav_1h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '2 hours') 
			/ COUNT(*) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '2 hours')), 8) AS movav_2h,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_3h
	FROM ticks
	WHERE statistics_close_time >= NOW() - INTERVAL '3 hours'
	GROUP BY symbol
	HAVING COUNT(*) > 10300;
	-- at least roughly 3 * 60 * 60 (3 hours) values
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
		ROUND(((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_close_time)) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '6 hours'))*60*60*6)::NUMERIC, 8) AS slope_6h,
		ROUND(((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_close_time)) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '12 hours'))*60*60*12)::NUMERIC, 8) AS slope_12h,
		ROUND(((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_close_time)) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '18 hours'))*60*60*18)::NUMERIC, 8) AS slope_18h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_close_time))*60*60*24)::NUMERIC, 8) AS slope_1d,
		ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '6 hours') 
			/ COUNT(*) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '6 hours')), 8) AS movav_6h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '12 hours') 
			/ COUNT(*) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '12 hours')), 8) AS movav_12h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '18 hours') 
			/ COUNT(*) FILTER (WHERE statistics_close_time >= NOW() - INTERVAL '18 hours')), 8) AS movav_18h,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_1d
	FROM ticks
	WHERE statistics_close_time >= NOW() - INTERVAL '1 day'	
	GROUP BY symbol
	HAVING COUNT(*) > 86200;
	-- at least roughly 24 * 60 * 60 (24 hours) values
END;
$$
LANGUAGE plpgsql STRICT;



CREATE OR REPLACE FUNCTION get_latest_ma10m_ma30m_crossing()
RETURNS TABLE (symbol TEXT, event_time TIMESTAMPTZ, slope10m NUMERIC, slope30m NUMERIC)
AS
$$
BEGIN
	RETURN QUERY SELECT DISTINCT ON (r.symbol) r.symbol, MAX(r.created_on), r.slope10m, r.slope30m FROM recommendation r
		WHERE ROUND(r.movav10m, 2) = ROUND(r.movav30m, 2) AND r.created_on >= NOW() - INTERVAL '12 hours'
		GROUP BY r.symbol, r.slope10m, r.slope30m
		ORDER BY r.symbol, MAX(r.created_on) DESC;
END;
$$
LANGUAGE plpgsql STRICT;




CREATE OR REPLACE FUNCTION get_latest_ma30m_ma1h_crossing()
RETURNS TABLE (symbol TEXT, event_time TIMESTAMPTZ, slope30m NUMERIC, slope1h NUMERIC)
AS
$$
BEGIN
	RETURN QUERY SELECT DISTINCT ON (r.symbol) r.symbol, MAX(r.created_on), r.slope30m, r.slope1h FROM recommendation r
		WHERE ROUND(r.movav30m, 2) = ROUND(r.movav1h, 2) AND r.created_on >= NOW() - INTERVAL '12 hours'
		GROUP BY r.symbol, r.slope30m, r.slope1h
		ORDER BY r.symbol, MAX(r.created_on) DESC;
END;
$$
LANGUAGE plpgsql STRICT;


CREATE OR REPLACE FUNCTION public.get_latest_ma1h_ma3h_crossing(
	)
    RETURNS TABLE(symbol text, event_time timestamp with time zone, slope1h numeric, slope3h numeric) 
    LANGUAGE 'plpgsql'

    COST 100
    VOLATILE STRICT 
    ROWS 1000
    
AS $BODY$
BEGIN
	RETURN QUERY SELECT DISTINCT ON (r.symbol) r.symbol, MAX(r.created_on), r.slope1h, r.slope3h FROM recommendation r
		WHERE ROUND(r.movav1h, 2) = ROUND(r.movav3h, 2) AND r.created_on >= NOW() - INTERVAL '12 hours'
		GROUP BY r.symbol, r.slope1h, r.slope3h
		ORDER BY r.symbol, MAX(r.created_on) DESC;
END;
$BODY$;



CREATE OR REPLACE FUNCTION get_price_on(p_symbol TEXT, p_time TIMESTAMPTZ)
RETURNS NUMERIC AS
$$
	DECLARE i_avg_price NUMERIC;
BEGIN
	IF p_time > NOW() THEN
		p_time = NOW();
	END IF;

	-- Calculate live over average of 20 records
	SELECT ROUND(AVG((best_ask_price + best_bid_price)/2), 8) INTO i_avg_price FROM (
		SELECT best_ask_price, best_bid_price
			FROM book_ticks 
			WHERE created_on <= p_time AND symbol = p_symbol
			ORDER BY created_on DESC
			LIMIT 20
		) a;
		
	-- Get from historical data
	IF i_avg_price IS NULL THEN
		RAISE NOTICE 'Not Found';
		SELECT ROUND(current_day_close_price, 8) INTO i_avg_price
			FROM ticks
			WHERE created_on BETWEEN DATE_TRUNC('second', p_time) AND DATE_TRUNC('second', p_time + INTERVAL '1 second') - INTERVAL '1 microsecond'
				AND symbol = p_symbol;
	END IF;

	RETURN i_avg_price;
END;
$$
LANGUAGE plpgsql STRICT STABLE;


--insert into symbols (name, is_collection_active, is_trading_active) values ('BTCUSDT', true, true);


-- FUNCTION: public.get_price_on(text, timestamp with time zone)

-- DROP FUNCTION public.get_price_on(text, timestamp with time zone);

CREATE OR REPLACE FUNCTION public.get_lowest_price(
	p_symbol text,
	p_time timestamp with time zone)
	RETURNS numeric
	LANGUAGE 'plpgsql'

	COST 100
	STABLE STRICT 

AS $BODY$
	DECLARE i_avg_price NUMERIC;
BEGIN
	IF p_time > NOW() THEN
		p_time = NOW();
	END IF;

	SELECT MIN(current_day_close_price) INTO i_avg_price FROM ticks WHERE symbol = p_symbol AND statistics_close_time > p_time;

	RETURN i_avg_price;
END;
$BODY$;



CREATE OR REPLACE FUNCTION public.select_last_orders(
	p_symbol text)
    RETURNS TABLE(r_binance_placed_order_id bigint, r_transaction_time timestamp with time zone, r_symbol text, r_side text, r_price numeric, r_quantity numeric, r_consumed numeric, r_consumed_price numeric) 
    LANGUAGE 'plpgsql'

    COST 100
    VOLATILE STRICT 
    ROWS 1000
    
AS $BODY$
BEGIN
	RETURN QUERY SELECT binance_placed_order_id, transaction_time, symbol, bpo.side, bot.price,
			quantity, ROUND(consumed::NUMERIC, 8), ROUND(consumed_price::NUMERIC, 8) FROM binance_order_trade bot
		INNER JOIN binance_placed_order bpo ON bpo.id = bot.binance_placed_order_id
		WHERE bpo.symbol = p_symbol AND consumed < quantity AND bpo.executed_quantity > 0
		ORDER BY binance_placed_order_id DESC;
END;
$BODY$;



CREATE OR REPLACE FUNCTION public.report_profits(
	p_symbol text)
    RETURNS TABLE(r_date date, r_profit numeric, r_increase numeric) 
    LANGUAGE 'plpgsql'

    COST 100
    STABLE STRICT 
    ROWS 1000
    
AS $BODY$
BEGIN
	RETURN QUERY 
			SELECT r_transaction_time, profit, profit - lag_profit FROM
			(
				SELECT r_transaction_time, profit,
						COALESCE(ROUND(LAG(profit, 1) OVER (ORDER BY r_transaction_time ASC), 8), 0) AS lag_profit
						FROM
							(SELECT r_transaction_time::DATE, MAX(r_walking_profit) profit
								FROM 
							(
								select * from report_walking_profit(p_symbol)
								WHERE  r_side = 'Sell'
							) b
							WHERE r_walking_profit > 0 AND (r_sum_buy < r_lead_buy OR r_lead_buy IS NULL)
							GROUP BY r_transaction_time::DATE) a
				) c
			WHERE profit > lag_profit
			ORDER BY r_transaction_time::DATE ASC;
END;
$BODY$;




CREATE OR REPLACE FUNCTION public.report_walking_profit(
	p_symbol text)
    RETURNS TABLE(r_transaction_time timestamp with time zone, r_symbol text, r_side text, r_turnover numeric, r_sum_buy numeric, r_sum_sell numeric, r_walking_profit numeric, r_lead_buy numeric, r_lead_sell numeric) 
    LANGUAGE 'plpgsql'

    COST 100
    STABLE STRICT 
    ROWS 1000
    
AS $BODY$
BEGIN

	RETURN QUERY
	WITH data AS (
		SELECT transaction_time, bpo.symbol, bpo.side, bot.price * bot.quantity AS turnover FROM binance_order_trade bot
		INNER JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
		WHERE bpo.symbol = p_symbol
		ORDER BY bpo.transaction_time ASC
	)
	SELECT transaction_time,
		symbol,
		side, 
		ROUND(turnover, 8) AS turnover, 
		ROUND(sum_buy, 8) AS sum_buy,
		COALESCE(ROUND(sum_sell, 8), 0) AS sum_sell,
		COALESCE(ROUND(sum_sell - sum_buy, 8), 0) AS walking_profit,
		ROUND(LEAD(sum_buy, 1) OVER (ORDER BY transaction_time ASC), 8) AS lead_buy,
		ROUND(LEAD(sum_sell, 1) OVER (ORDER BY transaction_time ASC), 8) AS lead_sell
			FROM (
				SELECT transaction_time, symbol, side, turnover,
					SUM(turnover) FILTER (WHERE side = 'Buy') OVER (ORDER BY transaction_time ASC ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) sum_buy,
					SUM(turnover) FILTER (WHERE side = 'Sell') OVER (ORDER BY transaction_time ASC ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) sum_sell
				FROM data
	) a
	ORDER BY transaction_time ASC, lead_buy ASC NULLS LAST, lead_sell ASC NULLS LAST;
END;
$BODY$;


CREATE OR REPLACE FUNCTION public.report_last_decisions(
	)
    RETURNS TABLE(r_decision text, r_event_time timestamp with time zone) 
    LANGUAGE 'plpgsql'

    COST 100
    STABLE STRICT 
    ROWS 1000
    
AS $BODY$
BEGIN
	RETURN QUERY SELECT DISTINCT ON (decision) decision, event_time
					FROM recommendation
					WHERE event_time > NOW() - INTERVAL '24 hours'
					ORDER BY decision, event_time DESC;
END;
$BODY$;



CREATE OR REPLACE FUNCTION public.get_highest_price(
	p_symbol text,
	p_time timestamp with time zone)
    RETURNS numeric
    LANGUAGE 'plpgsql'

    COST 100
    STABLE STRICT 
    
AS $BODY$
	DECLARE i_avg_price NUMERIC;
BEGIN
	IF p_time > NOW() THEN
		p_time = NOW();
	END IF;

	SELECT MAX(current_day_close_price) INTO i_avg_price FROM ticks WHERE symbol = p_symbol AND statistics_close_time > p_time;

	RETURN i_avg_price;
END;
$BODY$;

ALTER FUNCTION public.get_highest_price(text, timestamp with time zone)
    OWNER TO postgres;


CREATE OR REPLACE FUNCTION public.get_last_decisions(
	)
    RETURNS TABLE(r_symbol text, r_action int4, r_event_time timestamp) 
    LANGUAGE 'plpgsql'

    COST 100
    STABLE STRICT 
    ROWS 1000
    
AS $BODY$
BEGIN
	RETURN QUERY SELECT DISTINCT ON (symbol, action) symbol, action, created_on
					FROM recommendations
					WHERE created_on > NOW() - INTERVAL '24 hours'
					ORDER BY symbol, action, created_on DESC;
END;
$BODY$;

-- insert into symbols (name, is_collection_active, is_trading_active) values ('LINKUSDT', true, true)