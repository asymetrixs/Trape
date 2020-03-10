using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using trape.cli.trader.Cache.Buffers;

namespace trape.cli.trader.Cache
{
    public class Buffer
    {
        public string Symbol { get; private set; }

        public Short3SecBuffer Short3Sec { get; private set; }


        public Buffer(string symbol)
        {

        }       
    }
}
