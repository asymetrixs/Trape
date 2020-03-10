namespace trape.cli.trader.Cache.Buffers
{
    public class Short3SecBuffer
    {
        public string Symbol { get; private set; }

        public decimal Seconds5 { get; private set; }

        public decimal Seconds10 { get; private set; }

        public decimal Seconds15 { get; private set; }

        public Short3SecBuffer(string symbol, decimal seconds5, decimal seconds10, decimal seconds15)
        {
            this.Symbol = symbol;
            this.Seconds5 = seconds5;
            this.Seconds10 = seconds10;
            this.Seconds15 = seconds15;
        }
    }
}
