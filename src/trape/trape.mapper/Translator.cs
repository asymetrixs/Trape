using Binance.Net.Objects;
using Binance.Net.Objects.Sockets;
using System;
using System.Collections.Generic;
using trape.datalayer.Models;

namespace trape.mapper
{
    public static class Translator
    {
        public static PlacedOrder Translate(BinancePlacedOrder binancePlacedOrder)
        {
            var placedOrder = new PlacedOrder()
            {
                ClientOrderId = binancePlacedOrder.ClientOrderId,
                CummulativeQuoteQuantity = binancePlacedOrder.CummulativeQuoteQuantity,
                ExecutedQuantity = binancePlacedOrder.ExecutedQuantity,
                MarginBuyBorrowAmount = binancePlacedOrder.MarginBuyBorrowAmount,
                MarginBuyBorrowAsset = binancePlacedOrder.MarginBuyBorrowAsset,
                OrderId = binancePlacedOrder.OrderId,
                OrderListId = binancePlacedOrder.OrderListId,
                OriginalClientOrderId = binancePlacedOrder.OriginalClientOrderId,
                OriginalQuantity = binancePlacedOrder.OriginalQuantity,
                OriginalQuoteOrderQuantity = binancePlacedOrder.OriginalQuoteOrderQuantity,
                Price = binancePlacedOrder.Price,
                Side = (datalayer.Enums.OrderSide)(int)binancePlacedOrder.Side,
                Status = (datalayer.Enums.OrderStatus)(int)binancePlacedOrder.Status,
                StopPrice = binancePlacedOrder.StopPrice,
                Symbol = binancePlacedOrder.Symbol,
                TimeInForce = (datalayer.Enums.TimeInForce)(int)binancePlacedOrder.TimeInForce,
                TransactionTime = binancePlacedOrder.TransactTime,
                Type = (datalayer.Enums.OrderType)(int)binancePlacedOrder.Type
            };

            foreach (var fill in binancePlacedOrder.Fills)
            {
                placedOrder.Fills.Add(Translate(fill, placedOrder));
            }

            return placedOrder;
        }

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

        public static IEnumerable<Balance> Translate(IEnumerable<BinanceStreamBalance> binanceStreamBalances)
        {
            var list = new List<Balance>();
            foreach (var streamBalance in binanceStreamBalances)
            {
                list.Add(new Balance()
                {
                    Asset = streamBalance.Asset,
                    Free = streamBalance.Free,
                    Locked = streamBalance.Locked,
                    Total = streamBalance.Total,
                    CreatedOn = DateTime.UtcNow
                });
            }

            return list;
        }

        public static BalanceUpdate Translate(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate)
        {
            return new BalanceUpdate()
            {
                Asset = binanceStreamBalanceUpdate.Asset,
                BalanceDelta = binanceStreamBalanceUpdate.BalanceDelta,
                ClearTime = binanceStreamBalanceUpdate.ClearTime
            };
        }

        public static OrderUpdate Translate(BinanceStreamOrderUpdate binanceStreamOrderUpdate)
        {
            return new OrderUpdate()
            {
                AccumulatedQuantityOfFilledTrades = binanceStreamOrderUpdate.AccumulatedQuantityOfFilledTrades,
                BuyerIsMaker = binanceStreamOrderUpdate.BuyerIsMaker,
                ClientOrderId = binanceStreamOrderUpdate.ClientOrderId,
                Commission = binanceStreamOrderUpdate.Commission,
                CommissionAsset = binanceStreamOrderUpdate.CommissionAsset,
                CummulativeQuoteQuantity = binanceStreamOrderUpdate.CummulativeQuoteQuantity,
                ExecutionType = (datalayer.Enums.ExecutionType)(int)binanceStreamOrderUpdate.ExecutionType,
                I = binanceStreamOrderUpdate.I,
                IcebergQuantity = binanceStreamOrderUpdate.IcebergQuantity,
                IsWorking = binanceStreamOrderUpdate.IsWorking,
                LastQuoteTransactedQuantity = binanceStreamOrderUpdate.LastQuoteTransactedQuantity,
                OrderCreationTime = binanceStreamOrderUpdate.OrderCreationTime,
                OrderId = binanceStreamOrderUpdate.OrderId,
                OrderListId = binanceStreamOrderUpdate.OrderListId,
                OriginalClientOrderId = binanceStreamOrderUpdate.OriginalClientOrderId,
                Price = binanceStreamOrderUpdate.Price,
                PriceLastFilledTrade = binanceStreamOrderUpdate.PriceLastFilledTrade,
                Quantity = binanceStreamOrderUpdate.Quantity,
                QuantityOfLastFilledTrade = binanceStreamOrderUpdate.QuantityOfLastFilledTrade,
                RejectReason = (datalayer.Enums.OrderRejectReason)(int)binanceStreamOrderUpdate.RejectReason,
                QuoteOrderQuantity = binanceStreamOrderUpdate.QuoteOrderQuantity,
                Side = (datalayer.Enums.OrderSide)(int)binanceStreamOrderUpdate.Side,
                Status = (datalayer.Enums.OrderStatus)(int)binanceStreamOrderUpdate.Status,
                StopPrice = binanceStreamOrderUpdate.StopPrice,
                Symbol = binanceStreamOrderUpdate.Symbol,
                TimeInForce = (datalayer.Enums.TimeInForce)(int)binanceStreamOrderUpdate.TimeInForce,
                TradeId = binanceStreamOrderUpdate.TradeId,
                Type = (datalayer.Enums.OrderType)(int)binanceStreamOrderUpdate.Type
            };
        }

        public static Tick Translate(BinanceStreamTick binanceStreamTick)
        {
            return new Tick()
            {
                BestAskPrice = binanceStreamTick.BestAskPrice,
                BestAskQuantity = binanceStreamTick.BestAskQuantity,
                BestBidPrice = binanceStreamTick.BestBidPrice,
                BestBidQuantity = binanceStreamTick.BestBidQuantity,
                CloseTradesQuantity = binanceStreamTick.CloseTradesQuantity,
                CurrentDayClosePrice = binanceStreamTick.CurrentDayClosePrice,
                FirstTradeId = binanceStreamTick.FirstTradeId,
                HighPrice = binanceStreamTick.HighPrice,
                LastTradeId = binanceStreamTick.LastTradeId,
                LowPrice = binanceStreamTick.LowPrice,
                OpenPrice = binanceStreamTick.OpenPrice,
                PrevDayClosePrice = binanceStreamTick.PrevDayClosePrice,
                PriceChange = binanceStreamTick.PriceChange,
                PriceChangePercentage = binanceStreamTick.PriceChangePercentage,
                StatisticsCloseTime = binanceStreamTick.StatisticsCloseTime,
                StatisticsOpenTime = binanceStreamTick.StatisticsOpenTime,
                Symbol = binanceStreamTick.Symbol,
                TotalTradedBaseAssetVolume = binanceStreamTick.TotalTradedBaseAssetVolume,
                TotalTradedQuoteAssetVolume = binanceStreamTick.TotalTradedQuoteAssetVolume,
                TotalTrades = binanceStreamTick.TotalTrades,
                WeightedAverage = binanceStreamTick.WeightedAverage
            };
        }

        public static Kline Translate(BinanceStreamKlineData binanceStreamKlineData)
        {
            return new Kline()
            {
                Close = binanceStreamKlineData.Data.Close,
                CloseTime = binanceStreamKlineData.Data.CloseTime,
                Final = binanceStreamKlineData.Data.Final,
                FirstTrade = binanceStreamKlineData.Data.FirstTrade,
                High = binanceStreamKlineData.Data.High,
                Interval = (datalayer.Enums.KlineInterval)((int)binanceStreamKlineData.Data.Interval),
                LastTrade = binanceStreamKlineData.Data.LastTrade,
                Low = binanceStreamKlineData.Data.Low,
                Open = binanceStreamKlineData.Data.Open,
                OpenTime = binanceStreamKlineData.Data.OpenTime,
                QuoteAssetVolume = binanceStreamKlineData.Data.QuoteAssetVolume,
                Symbol = binanceStreamKlineData.Data.Symbol,
                TakerBuyBaseAssetVolume = binanceStreamKlineData.Data.TakerBuyBaseAssetVolume,
                TakerBuyQuoteAssetVolume = binanceStreamKlineData.Data.TakerBuyQuoteAssetVolume,
                TradeCount = binanceStreamKlineData.Data.TradeCount,
                Volume = binanceStreamKlineData.Data.Volume
            };
        }

        public static BookTick Translate(BinanceBookTick binanceBookTick)
        {
            return new BookTick()
            {
                BestAskPrice = binanceBookTick.BestAskPrice,
                BestAskQuantity = binanceBookTick.BestAskQuantity,
                BestBidPrice = binanceBookTick.BestBidPrice,
                BestBidQuantity = binanceBookTick.BestBidQuantity,
                CreatedOn = DateTime.UtcNow,
                Symbol = binanceBookTick.Symbol,
                UpdateId = binanceBookTick.UpdateId
            };
        }
    }
}
