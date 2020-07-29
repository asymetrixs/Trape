select * from orders order by id desc
select * from order_lists
select * from client_order
select * from order_trades
select * from order_updates

delete from order_trades;
delete from order_updates;
delete from placed_orders;
delete from order_lists;
delete from client_order;
delete from orders;
--where client_order_id = '75467639bef04f8ba654c152a50d7373' FK_orders_client_order_client_order_id

select * from orders where client_order_id = '22cde4b28cbe4d3f8938e9efeaabbbbd'
{"23503: insert or update on table \"order_updates\" violates foreign key constraint
\"FK_order_updates_orders_order_id\""}

select * from account_infos