using Binance.Net.Objects.Spot.MarketData;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trape.Cli.trader.Cache.Models;
using Trape.Datalayer.Models;

namespace Trape.Cli.trader.Cache
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
        void Terminate();

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
        /// Updates a recommendation
        /// </summary>
        /// <param name="recommendation"></param>
        void UpdateRecommendation(Recommendation recommendation);

        /// <summary>
        /// Returns a recommendation for a <paramref name="symbol"/>.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        Recommendation GetRecommendation(string symbol);

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
        /// Returns exchange information for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>Exchange information</returns>
        BinanceSymbol? GetSymbolInfoFor(string symbol);

        /// <summary>
        /// Returns the last falling price
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        FallingPrice? GetLastFallingPrice(string symbol);

        /// <summary>
        /// Checks if enough data for processing is available
        /// </summary>
        /// <param name="symbol">Symbol to check</param>
        /// <returns></returns>
        bool IsReady(string symbol);

        /// <summary>
        /// Returns the change in percent in a given timespan compared to now.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="timespan">Interval</param>
        /// <returns></returns>
        decimal? Slope(string symbol, TimeSpan timespan);


        /// <summary>
        /// Returns the lowest price in the given timespan
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="timeSpan">Interval</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        decimal GetLowestPrice(string symbol, TimeSpan timespan);
    }
}
