SELECT count(*) from price;
SELECT * from price LIMIT 100;

SELECT regr_slope(price, EXTRACT(EPOCH FROM datetime)) FROM price

SELECT ROUND(a.trend::numeric, 10) AS "15 min",
	ROUND(b.trend::numeric, 10) AS "10 min",
	ROUND(c.trend::numeric, 10) AS "7 min",
	ROUND(d.trend::numeric, 10) AS "5 min",
	ROUND(e.trend::numeric, 10) AS "1 min" FROM
(SELECT regr_slope(price, EXTRACT(EPOCH FROM datetime)) as trend FROM price WHERE datetime BETWEEN NOW() - INTERVAL '15 minutes' AND NOW()) a,
(SELECT regr_slope(price, EXTRACT(EPOCH FROM datetime)) as trend FROM price WHERE datetime BETWEEN NOW() - INTERVAL '10 minutes' AND NOW()) b,
(SELECT regr_slope(price, EXTRACT(EPOCH FROM datetime)) as trend FROM price WHERE datetime BETWEEN NOW() - INTERVAL '7 minutes' AND NOW()) c,
(SELECT regr_slope(price, EXTRACT(EPOCH FROM datetime)) as trend FROM price WHERE datetime BETWEEN NOW() - INTERVAL '5 minutes' AND NOW()) d,
(SELECT regr_slope(price, EXTRACT(EPOCH FROM datetime)) as trend FROM price WHERE datetime BETWEEN NOW() - INTERVAL '1 minutes' AND NOW()) e



SELECT count(*) FROM candlestick;
SELECT * FROM candlestick;