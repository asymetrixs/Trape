using System;
using System.Collections.Generic;
using System.Text;

namespace trape.cli.trader.Fees
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
