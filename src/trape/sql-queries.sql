SELECT bpo.time_in_force, bpo.transaction_time, bpo.side, bpo.symbol, bot.price, bot.quantity, bot.commission, 
	bot.commission_asset, bot.consumed, ROUND(bot.consumed_price, 8) FROM binance_order_trade AS bot
LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
ORDER BY binance_placed_order_id DESC, bpo.id DESC;

select * from select_asset_status()
select * from current_statement()

select * from binance_order_trade order by binance_placed_order_id desc
update binance_order_trade set consumed = quantity, consumed_price = 6630 where consumed != quantity
update binance_order_trade set consumed_price = price where binance_placed_order_id != 278 AND consumed_price = 0
select * from binance_order_trade

delete from binance_placed_order;
delete from binance_order_trade;


--233
--	Bitcoin 0.44544746 0.44544746
-- Ethereum 8.16653543 8.16653543

SELECT * FROM fix_symbol_quantity('BTCUSDT', 0.04132487, 7133.00);

-- Pseudo panic drops
--6830 :04 -> 6817 :17	> 13 USD	13s		0.99809
--6845 :07 -> 6815 :23	> 30 USD	16s		0.99562
--6804 :57 -> 6797 :05	>  7 USD	 8s		0.99897
--6797 :08 -> 6787 :19	> 10 USD	11s		0.99853
--==> 20s & > 0.5%

SELECT symbol, * FROM recommendation
WHERE event_time > NOW() - INTERVAL '1 minute' AND slope5s < 0
	ORDER BY event_time ASC
	LIMIT 1

select * from recommendation
	where event_time between '2020-04-21 13:34:50.000 +00'::timestamptz and '2020-04-21 13:35:25.000 +00'::timestamptz
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