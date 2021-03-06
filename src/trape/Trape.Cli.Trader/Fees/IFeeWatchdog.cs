﻿namespace Trape.Cli.trader.Fees
{
    /// <summary>
    /// Interface for <c>FeeWatchdog</c>
    /// </summary>
    public interface IFeeWatchdog
    {
        void Start();

        void Terminate();
    }
}
