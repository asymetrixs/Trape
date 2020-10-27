using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot.MarketStream;
using Binance.Net.Objects.Spot.SpotData;
using Binance.Net.Objects.Spot.UserStream;
using System;
using System.Collections.Generic;
using trape.datalayer.Models;

namespace trape.mapper
{
    /// <summary>
    /// Translates Binance.Net objects into Trape objects
    /// </summary>
    public static class Translator
    {
        /// <summary>
        /// Translates from <c>BinancePlacedOrder</c> to <c>PlacedOrder</c>.
        /// </summary>
        /// <param name="binancePlacedOrder"></param>
        /// <returns></returns>
        public static PlacedOrder Translate(BinancePlacedOrder binancePlacedOrder)
        {
            var placedOrder = new PlacedOrder()
            {
                ClientOrderId = binancePlacedOrder.ClientOrderId,
                CreateTime = binancePlacedOrder.CreateTime,
                MarginBuyBorrowAmount = binancePlacedOrder.MarginBuyBorrowAmount,
                MarginBuyBorrowAsset = binancePlacedOrder.MarginBuyBorrowAsset,
                OrderId = binancePlacedOrder.OrderId,
                OrderListId = binancePlacedOrder.OrderListId,
                OriginalClientOrderId = binancePlacedOrder.OriginalClientOrderId,
                Price = binancePlacedOrder.Price,
                Quantity = binancePlacedOrder.Quantity,
                QuantityFilled = binancePlacedOrder.QuantityFilled,
                QuoteQuantity = binancePlacedOrder.QuoteQuantity,
                QuoteQuantityFilled = binancePlacedOrder.QuoteQuantityFilled,
                Side = (datalayer.Enums.OrderSide)(int)binancePlacedOrder.Side,
                Status = (datalayer.Enums.OrderStatus)(int)binancePlacedOrder.Status,
                StopPrice = binancePlacedOrder.StopPrice,
                Symbol = binancePlacedOrder.Symbol,
                TimeInForce = (datalayer.Enums.TimeInForce)(int)binancePlacedOrder.TimeInForce,
                Type = (datalayer.Enums.OrderType)(int)binancePlacedOrder.Type
            };

            foreach (var fill in binancePlacedOrder.Fills)
            {
                placedOrder.Fills.Add(Translate(fill, placedOrder));
            }

            return placedOrder;
        }

        /// <summary>
        /// Translates from <c>BinanceBalance</c> to <c>Balance</c>.
        /// </summary>
        /// <param name="binanceBalance"></param>
        /// <param name="accountInfo"></param>
        /// <returns></returns>
        public static Balance Translate(BinanceBalance binanceBalance, AccountInfo accountInfo)
        {
            var balance = new Balance()
            {
                AccountInfo = accountInfo,
                Asset = binanceBalance.Asset,
                CreatedOn = DateTime.UtcNow,
                Free = binanceBalance.Free,
                Locked = binanceBalance.Locked,
                Total = binanceBalance.Total
            };

            return balance;
        }

        /// <summary>
        /// Translates from <c>BinanceOrderTrade</c> to <c>OrderTrade</c>.
        /// </summary>
        /// <param name="binanceOrderTrade"></param>
        /// <param name="placedOrder"></param>
        /// <returns></returns>
        public static OrderTrade Translate(BinanceOrderTrade binanceOrderTrade, PlacedOrder placedOrder = null)
        {
            return new OrderTrade()
            {
                Commission = binanceOrderTrade.Commission,
                CommissionAsset = binanceOrderTrade.CommissionAsset,
                Price = binanceOrderTrade.Price,
                Quantity = binanceOrderTrade.Quantity,
                TradeId = binanceOrderTrade.TradeId,
                PlacedOrder = placedOrder
            };
        }

        /// <summary>
        /// Translates from <c>BinanceStreamOrderList</c> to <c>OrderList</c>.
        /// </summary>
        /// <param name="binanceStreamOrderList"></param>
        /// <returns></returns>
        public static OrderList Translate(BinanceStreamOrderList binanceStreamOrderList)
        {
            var orderList = new OrderList()
            {
                ContingencyType = binanceStreamOrderList.ContingencyType,
                ListClientOrderId = binanceStreamOrderList.ListClientOrderId,
                ListOrderStatus = (datalayer.Enums.ListOrderStatus)(int)binanceStreamOrderList.ListOrderStatus,
                ListStatusType = (datalayer.Enums.ListStatusType)(int)binanceStreamOrderList.ListStatusType,
                OrderListId = binanceStreamOrderList.OrderListId,
                Symbol = binanceStreamOrderList.Symbol,
                TransactionTime = binanceStreamOrderList.TransactionTime
            };

            foreach (var ol in binanceStreamOrderList.Orders)
            {
                orderList.Orders.Add(Translate(ol, orderList));
            }

            return orderList;
        }

        /// <summary>
        /// Translates from <c>BinanceStreamOrderid</c> to <c>Order</c>.
        /// </summary>
        /// <param name="binanceStreamOrderId"></param>
        /// <param name="orderList"></param>
        /// <returns></returns>
        public static Order Translate(BinanceStreamOrderId binanceStreamOrderId, OrderList orderList = null)
        {
            return new Order()
            {
                ClientOrderId = binanceStreamOrderId.ClientOrderId,
                CreatedOn = DateTime.UtcNow,
                OrderId = binanceStreamOrderId.OrderId,
                OrderList = orderList,
                Symbol = binanceStreamOrderId.Symbol,
            };
        }

        /// <summary>
        /// Translates from <c>BinanceStreamBalance</c> to <c>Balance</c>.
        /// </summary>
        /// <param name="binanceStreamPositionsUpdate"></param>
        /// <returns></returns>
        public static IEnumerable<Balance> Translate(BinanceStreamPositionsUpdate binanceStreamPositionsUpdate)
        {
            var list = new List<Balance>();
            foreach (var balance in binanceStreamPositionsUpdate.Balances)
            {
                list.Add(new Balance()
                {
                    Asset = balance.Asset,
                    Free = balance.Free,
                    Locked = balance.Locked,
                    Total = balance.Total,
                    CreatedOn = binanceStreamPositionsUpdate.Time.ToUniversalTime()
                });
            }

            return list;
        }

        /// <summary>
        /// Translates from <c>BinanceStreamBalanceUpdate</c> to <c>BalanceUpdate</c>.
        /// </summary>
        /// <param name="binanceStreamBalanceUpdate"></param>
        /// <returns></returns>
        public static BalanceUpdate Translate(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate)
        {
            return new BalanceUpdate()
            {
                Asset = binanceStreamBalanceUpdate.Asset,
                BalanceDelta = binanceStreamBalanceUpdate.BalanceDelta,
                ClearTime = binanceStreamBalanceUpdate.ClearTime
            };
        }

        /// <summary>
        /// Translates from <c>BinanceStreamOrderUpdate</c> to <c>OrderUpdate</c>.
        /// </summary>
        /// <param name="binanceStreamOrderUpdate"></param>
        /// <returns></returns>
        public static OrderUpdate Translate(BinanceStreamOrderUpdate binanceStreamOrderUpdate)
        {
            return new OrderUpdate()
            {
                BuyerIsMaker = binanceStreamOrderUpdate.BuyerIsMaker,
                ClientOrderId = binanceStreamOrderUpdate.ClientOrderId,
                Commission = binanceStreamOrderUpdate.Commission,
                CommissionAsset = binanceStreamOrderUpdate.CommissionAsset,
                CreateTime = binanceStreamOrderUpdate.CreateTime,
                ExecutionType = (datalayer.Enums.ExecutionType)(int)binanceStreamOrderUpdate.ExecutionType,
                I = binanceStreamOrderUpdate.I,
                IcebergQuantity = binanceStreamOrderUpdate.IcebergQuantity,
                IsWorking = binanceStreamOrderUpdate.IsWorking,
                LastPriceFilled = binanceStreamOrderUpdate.LastPriceFilled,
                LastQuantityFilled = binanceStreamOrderUpdate.LastQuantityFilled,
                LastQuoteQuantity = binanceStreamOrderUpdate.LastQuoteQuantity,
                OrderId = binanceStreamOrderUpdate.OrderId,
                OrderListId = binanceStreamOrderUpdate.OrderListId,
                OriginalClientOrderId = binanceStreamOrderUpdate.OriginalClientOrderId,
                Price = binanceStreamOrderUpdate.Price,
                Quantity = binanceStreamOrderUpdate.Quantity,
                QuantityFilled = binanceStreamOrderUpdate.QuantityFilled,
                QuoteQuantity = binanceStreamOrderUpdate.QuoteQuantity,
                QuoteQuantityFilled = binanceStreamOrderUpdate.QuoteQuantityFilled,
                RejectReason = (datalayer.Enums.OrderRejectReason)(int)binanceStreamOrderUpdate.RejectReason,
                Side = (datalayer.Enums.OrderSide)(int)binanceStreamOrderUpdate.Side,
                Status = (datalayer.Enums.OrderStatus)(int)binanceStreamOrderUpdate.Status,
                StopPrice = binanceStreamOrderUpdate.StopPrice,
                Symbol = binanceStreamOrderUpdate.Symbol,
                TimeInForce = (datalayer.Enums.TimeInForce)(int)binanceStreamOrderUpdate.TimeInForce,
                TradeId = binanceStreamOrderUpdate.TradeId,
                Type = (datalayer.Enums.OrderType)(int)binanceStreamOrderUpdate.Type,
                UpdateTime = binanceStreamOrderUpdate.UpdateTime
            };
        }

        /// <summary>
        /// Translates from <c>IBinanceTick</c> to <c>Tick</c>.
        /// </summary>
        /// <param name="binanceStreamTick"></param>
        /// <returns></returns>
        public static Tick Translate(IBinanceTick binanceStreamTick)
        {
            return new Tick()
            {
                AskPrice = binanceStreamTick.AskPrice,
                AskQuantity = binanceStreamTick.AskQuantity,
                BaseVolume = binanceStreamTick.BaseVolume,
                BidPrice = binanceStreamTick.BidPrice,
                BidQuantity = binanceStreamTick.BidQuantity,
                CloseTime = binanceStreamTick.CloseTime,
                FirstTradeId = binanceStreamTick.FirstTradeId,
                HighPrice = binanceStreamTick.HighPrice,
                LastPrice = binanceStreamTick.LastPrice,
                LastQuantity = binanceStreamTick.LastQuantity,
                LastTradeId = binanceStreamTick.LastTradeId,
                LowPrice = binanceStreamTick.LowPrice,
                OpenPrice = binanceStreamTick.OpenPrice,
                OpenTime = binanceStreamTick.OpenTime,
                PrevDayClosePrice = binanceStreamTick.PrevDayClosePrice,
                PriceChange = binanceStreamTick.PriceChange,
                PriceChangePercent = binanceStreamTick.PriceChangePercent,
                QuoteVolume = binanceStreamTick.QuoteVolume,
                Symbol = binanceStreamTick.Symbol,
                TotalTrades = binanceStreamTick.TotalTrades,
                WeightedAveragePrice = binanceStreamTick.WeightedAveragePrice
            };
        }

        /// <summary>
        /// Translates from <c>IBinanceStreamKlineData</c> to <c>Kline</c>.
        /// </summary>
        /// <param name="binanceStreamKlineData"></param>
        /// <returns></returns>
        public static Kline Translate(IBinanceStreamKlineData binanceStreamKlineData)
        {
            return new Kline()
            {
                BaseVolume = binanceStreamKlineData.Data.BaseVolume,
                Close = binanceStreamKlineData.Data.Close,
                CloseTime = binanceStreamKlineData.Data.CloseTime,
                Final = binanceStreamKlineData.Data.Final,
                FirstTradeId = binanceStreamKlineData.Data.FirstTrade,
                High = binanceStreamKlineData.Data.High,
                Interval = (datalayer.Enums.KlineInterval)((int)binanceStreamKlineData.Data.Interval),
                LastTradeId = binanceStreamKlineData.Data.LastTrade,
                Low = binanceStreamKlineData.Data.Low,
                Open = binanceStreamKlineData.Data.Open,
                OpenTime = binanceStreamKlineData.Data.OpenTime,
                QuoteVolume = binanceStreamKlineData.Data.QuoteVolume,
                Symbol = binanceStreamKlineData.Symbol,
                TakerBuyBaseVolume = binanceStreamKlineData.Data.TakerBuyBaseVolume,
                TakerBuyQuoteVolume = binanceStreamKlineData.Data.TakerBuyQuoteVolume,
                TradeCount = binanceStreamKlineData.Data.TradeCount
            };
        }

        /// <summary>
        /// Translates from <c>BinanceBookTick</c> to <c>BookTick</c>.
        /// </summary>
        /// <param name="binanceBookTick"></param>
        /// <returns></returns>
        public static BookPrice Translate(BinanceStreamBookPrice binanceStreamBookPrice)
        {
            return new BookPrice()
            {
                BestAskPrice = binanceStreamBookPrice.BestAskPrice,
                BestAskQuantity = binanceStreamBookPrice.BestAskQuantity,
                BestBidPrice = binanceStreamBookPrice.BestBidPrice,
                BestBidQuantity = binanceStreamBookPrice.BestBidQuantity,
                Symbol = binanceStreamBookPrice.Symbol,
                UpdateId = binanceStreamBookPrice.UpdateId
            };
        }
    }
}
