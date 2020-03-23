using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace trape.cli.trader.trade
{
    public interface ITrader : IDisposable
    {
        Task Start();

        void Stop();
    }
}
