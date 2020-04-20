SELECT bpo.time_in_force, bpo.transaction_time, bpo.side, bpo.symbol, bot.price, bot.quantity, bot.commission, 
	bot.commission_asset, bot.consumed, ROUND(bot.consumed_price, 8) FROM binance_order_trade AS bot
LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
ORDER BY binance_placed_order_id DESC, bpo.id DESC;

select * from select_asset_status()
select * from current_statement()


update binance_order_trade set consumed = quantity, consumed_price = 6630 where consumed != quantity
update binance_order_trade set consumed_price = price where binance_placed_order_id != 278 AND consumed_price = 0
select * from binance_order_trade

delete from binance_placed_order;
delete from binance_order_trade;


--233
--	Bitcoin 0.44544746 0.44544746
-- Ethereum 8.16653543 8.16653543

SELECT * FROM fix_symbol_quantity('BTCUSDT', 0.04132487, 7133.00);


select event_time::date, count(*) filter (where slope1h>0.004) AS higher, count(*) filter (where slope1h < 0.004) AS lower from recommendation 
where slope1h > 0.004 OR slope1h < 0.004 ANd symbol = 'BTCUSDT'
group by event_time::date
order by event_time::date

select *from recommendation
	where event_time between '2020-04-12 20:50:00.000 +00'::timestamptz and '2020-04-12 20:50:59.000 +00'::timestamptz
		AND symbol = 'BTCUSDT'
		ORDER BY event_time desc
		
select *from recommendation
	where event_time between '2020-04-16 10:36:00.000 +00'::timestamptz and '2020-04-16 10:36:59.000 +00'::timestamptz
		AND symbol = 'BTCUSDT'
		ORDER BY event_time desc
		
		
 /*
             * Do not buy when price > movav6h, do buy when price < movav6h
             * Do buy when slope1h > slope6h and slope1h > 0.03
             * Sell when slope1h < 0 and slope6h < 0 and movav2h <= movav6h
             * Do not buy when slope1h < slope6h AND slope6h < -0.005
             * Sell if slope1h < 0 and movav1h = movav6h
             * Buy if slope1h > 0 and movav1h = movav6h
             * Do net sell if slope6h > 0.012
             */