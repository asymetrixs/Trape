using Binance.Net.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using trape.cli.trader.Cache.Models;

namespace trape.cli.trader.Cache
{
    /// <summary>
    /// Interface for buffer
    /// </summary>
    public interface IBuffer : IDisposable
    {
        /// <summary>
        /// Starts a buffer
        /// </summary>
        /// <returns></returns>
        Task Start();

        /// <summary>
        /// Stops a buffer
        /// </summary>
        void Finish();

        /// <summary>
        /// Returns the available symbols the buffer has data for
        /// </summary>
        /// <returns>List of symbols</returns>
        IEnumerable<string> GetSymbols();

        /// <summary>
        /// Returns the latest ask price for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>Ask price of the symbol</returns>
        decimal GetAskPrice(string symbol);

        /// <summary>
        /// Returns the latest bid price for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>Bid price of the symol</returns>
        decimal GetBidPrice(string symbol);

        /// <summary>
        /// Stores open orders
        /// </summary>
        /// <param name="openOrder"></param>
        void AddOpenOrder(OpenOrder openOrder);

        /// <summary>
        /// Removes an open order
        /// </summary>
        /// <param name="clientOrderId">Id of open order</param>
        void RemoveOpenOrder(string clientOrderId);

        /// <summary>
        /// Returns the currently blocked
        /// </summary>
        decimal GetOpenOrderValue(string symbol);

        /// <summary>
        /// Stats3s
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        Stats3s Stats3sFor(string symbol);

        /// <summary>
        /// Stats15s
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        Stats15s Stats15sFor(string symbol);

        /// <summary>
        /// Stats2m
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        Stats2m Stats2mFor(string symbol);

        /// <summary>
        /// Stats10m
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        Stats10m Stats10mFor(string symbol);

        /// <summary>
        /// Stats2h
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        Stats2h Stats2hFor(string symbol);

        /// <summary>
        /// Returns exchange information for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>Exchange information</returns>
        BinanceSymbol GetSymbolInfoFor(string symbol);

        /// <summary>
        /// Returns the last time moving average 10m and moving average 30m were crossing
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        LatestMA10mAndMA30mCrossing GetLatest10mAnd30mCrossing(string symbol);

        /// <summary>
        /// Returns the last time moving average 1h and moving average 3h were crossing
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        LatestMA1hAndMA3hCrossing GetLatest1hAnd3hCrossing(string symbol);

        /// <summary>
        /// Returns the last falling price
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        public FallingPrice GetLastFallingPrice(string symbol);
    }
}
