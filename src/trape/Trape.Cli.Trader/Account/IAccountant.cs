﻿using Binance.Net.Objects.Spot.SpotData;
using System;
using System.Threading.Tasks;

namespace Trape.Cli.trader.Account
{
    public interface IAccountant : IDisposable
    {
        #region Methods

        Task Start();

        Task Terminate();

        Task<BinanceBalance?> GetBalance(string asset);

        #endregion
    }
}
