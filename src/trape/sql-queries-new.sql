

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
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '5 seconds'))::NUMERIC, 8) AS slope_5s,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '10 seconds'))::NUMERIC, 8) AS slope_10s,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '15 seconds'))::NUMERIC, 8) AS slope_15s,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)))::NUMERIC, 8) AS slope_30s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '5 seconds') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '5 seconds')), 8) AS movav_5s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '10 seconds') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '10 seconds')), 8) AS movav_10s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '15 seconds') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '15 seconds')), 8) AS movav_15s,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) / COUNT(*)), 8) AS movav_30s
	FROM book_ticks
	WHERE created_on >= NOW() - INTERVAL '30 seconds'
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
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '45 seconds'))::NUMERIC, 8) AS slope_45s,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '1 minute'))::NUMERIC, 8) AS slope_1m,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '2 minutes'))::NUMERIC, 8) AS slope_2m,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)))::NUMERIC, 8) AS slope_3m,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '45 second') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '45 second')), 8) AS movav_45s,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '1 minute') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '1 minute')), 8) AS movav_1m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '2 minutes') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '2 minutes')), 8) AS movav_2m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) / COUNT(*)), 8) AS movav_3m
	FROM book_ticks
	WHERE created_on >= NOW() - INTERVAL '3 minutes'
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
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '5 minutes'))::NUMERIC, 8) AS slope_5m,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '7 minutes'))::NUMERIC, 8) AS slope_7m,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)) FILTER (WHERE created_on >= NOW() - INTERVAL '10 minutes'))::NUMERIC, 8) AS slope_10,
		ROUND((REGR_SLOPE(ROUND((best_ask_price + best_bid_price ) /2, 8), EXTRACT(EPOCH FROM created_on)))::NUMERIC, 8) AS slope_15m,
		ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '5 minutes') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '5 minutes')), 8) AS movav_5m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '7 minutes') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '7 minutes')), 8) AS movav_7m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) FILTER (WHERE created_on >= NOW() - INTERVAL '10 minutes') 
			/ COUNT(*) FILTER (WHERE created_on >= NOW() - INTERVAL '10 minutes')), 8) AS movav_10m,
			ROUND((SUM(ROUND((best_ask_price + best_bid_price ) /2, 8)) / COUNT(*)), 8) AS movav_15m
	FROM book_ticks
	WHERE created_on >= NOW() - INTERVAL '15 minutes'
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
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_open_time)) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '30 minutes'))::NUMERIC, 8) AS slope_30m,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_open_time)) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '1 hour'))::NUMERIC, 8) AS slope_1h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_open_time)) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '2 hours'))::NUMERIC, 8) AS slope_2h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_open_time)))::NUMERIC, 8) AS slope_3h,
		ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '30 minutes') 
			/ COUNT(*) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '30 minutes')), 8) AS movav_30m,
			ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '1 hours') 
			/ COUNT(*) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '1 hours')), 8) AS movav_1h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '2 hours') 
			/ COUNT(*) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '2 hours')), 8) AS movav_2h,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_3h
	FROM ticks
	WHERE statistics_open_time >= NOW() - INTERVAL '3 hours'
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
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_open_time)) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '6 hours'))::NUMERIC, 8) AS slope_6h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_open_time)) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '12 hours'))::NUMERIC, 8) AS slope_12h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_open_time)) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '18 hours'))::NUMERIC, 8) AS slope_18h,
		ROUND((REGR_SLOPE(current_day_close_price, EXTRACT(EPOCH FROM statistics_open_time)))::NUMERIC, 8) AS slope_1d,
		ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '6 hours') 
			/ COUNT(*) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '6 hours')), 8) AS movav_6h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '12 hours') 
			/ COUNT(*) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '12 hours')), 8) AS movav_12h,
			ROUND((SUM(current_day_close_price) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '18 hours') 
			/ COUNT(*) FILTER (WHERE statistics_open_time >= NOW() - INTERVAL '18 hours')), 8) AS movav_18h,
			ROUND((SUM(current_day_close_price) / COUNT(*)), 8) AS movav_1d
	FROM ticks
	WHERE statistics_open_time >= NOW() - INTERVAL '1 day'
	GROUP BY symbol;
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

