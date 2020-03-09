using Binance.Net.Objects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace trape.cli.collector.DataCollection
{
    public interface ICollectionManager
    {
        Task Run();

        void Terminate();

        Task Save(BinanceStreamTick bst);

        Task Save(BinanceStreamKlineData bskd);
    }
}
