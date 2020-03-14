namespace trape.cli.trader.Cache.Trends
{
    public class Trend3Seconds : ITrend
    {
        public string Symbol { get; private set; }

        public int DataBasis { get; private set; }

        public decimal Seconds5 { get; private set; }

        public decimal Seconds10 { get; private set; }

        public decimal Seconds15 { get; private set; }

        public decimal Seconds30 { get; private set; }

        public Trend3Seconds(string symbol, int dataBasis, decimal seconds5, decimal seconds10, decimal seconds15, decimal seconds30)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Seconds5 = seconds5;
            this.Seconds10 = seconds10;
            this.Seconds15 = seconds15;
            this.Seconds30 = seconds30;
        }

        public bool IsValid()
        {
            // Roughly 30 (30 seconds)
            return this.DataBasis > 25;
        }
    }
}
