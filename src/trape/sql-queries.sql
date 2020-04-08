SELECT bpo.time_in_force, bpo.transaction_time, bpo.side, bpo.symbol, bot.* FROM binance_order_trade bot
LEFT JOIN binance_placed_order bpo ON bot.binance_placed_order_id = bpo.id
ORDER BY binance_placed_order_id DESC, id DESC;

select * from select_asset_status()
--233
--	Bitcoin 0.44544746 0.44544746
-- Ethereum 8.16653543 8.16653543

delete from binance_order_trade where binance_placed_order_id = 226;
delete from binance_placed_order where id = 226;



select * from binance_order_trade order by binance_placed_order_id DESC;

select * from binance_order_trade
select * from "order" order by id desc;
select * from stats_2h()

select * from select_last_orders('ETHUSDT')
select * from binance_placed_order
--delete from binance_placed_order;delete from binance_order_trade;

SELECT max(id) from binance_placed_order



delete from binance_placed_order;
delete from binance_order_trade;
SELECT * FROM fix_symbol_quantity('BTCUSDT', 0.01053939, 7343.58);
SELECT * FROM fix_symbol_quantity('ETHUSDT', 0, 170.59);
0.00009038









-- FUNCTION: public.select_asset_status()

-- DROP FUNCTION public.select_asset_status();

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

ALTER FUNCTION public.select_asset_status()
    OWNER TO trape;

select * from select_asset_status()
--233
--	Bitcoin 0.44544746 0.44544746
-- Ethereum 8.16653543 8.16653543
