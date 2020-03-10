using System;
using System.Collections.Generic;
using System.Text;

namespace trape.cli.trader.Cache
{
    public interface ISynchronizer
    {
        void Start();

        void Stop();
    }
}
