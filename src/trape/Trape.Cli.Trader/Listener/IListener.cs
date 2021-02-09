using Binance.Net.Objects.Spot.MarketData;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Trape.Cli.trader.Listener.Models;
using Trape.Datalayer.Models;

namespace Trape.Cli.trader.Listener
{
    /// <summary>
    /// Interface for buffer
    /// </summary>
    public interface IListener : IDisposable
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
        /// Informs about new assets
        /// </summary>
        IObservable<BinanceSymbol> NewAssets { get; }
    }
}
