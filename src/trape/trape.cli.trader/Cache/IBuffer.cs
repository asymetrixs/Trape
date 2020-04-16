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
        /// Holds ultra short term stats
        /// </summary>
        IEnumerable<Stats3s> Stats3s { get; }

        /// <summary>
        /// Holds short term stats
        /// </summary>
        IEnumerable<Stats15s> Stats15s { get; }

        /// <summary>
        /// Holds medium term stats
        /// </summary>
        IEnumerable<Stats2m> Stats2m { get; }

        /// <summary>
        /// Holds long term stats
        /// </summary>
        IEnumerable<Stats10m> Stats10m { get; }

        /// <summary>
        /// Holds ultra long term stats
        /// </summary>
        IEnumerable<Stats2h> Stats2h { get; }

        /// <summary>
        /// Holds current prices
        /// </summary>
        IEnumerable<CurrentPrice> CurrentPrices { get; }

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
        /// Returns exchange information for a symbol
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>Exchange information</returns>
        BinanceSymbol GetSymbolInfoFor(string symbol);
    }
}
