﻿using System;

namespace Trape.Cli.Trader.Team
{
    /// <summary>
    /// Interface for the <c>TradingTeam</c>
    /// </summary>
    public interface ITradingTeam : IDisposable
    {
        /// <summary>
        /// Starts the <c>TradingTeam</c>
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the <c>TradingTeam</c>
        /// </summary>
        /// <returns></returns>
        void Terminate();
    }
}
