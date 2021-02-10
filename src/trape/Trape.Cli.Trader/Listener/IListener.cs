﻿using Binance.Net.Objects.Spot.MarketData;
using System;
using System.Threading.Tasks;

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
        /// Informs about new assets
        /// </summary>
        IObservable<BinanceSymbol> NewAssets { get; }

        /// <summary>
        /// Informs about new exchange infos
        /// </summary>
        IObservable<BinanceExchangeInfo> NewExchangeInfo { get; }
    }
}
