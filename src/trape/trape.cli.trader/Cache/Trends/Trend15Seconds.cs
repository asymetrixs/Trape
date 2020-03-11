namespace trape.cli.trader.Cache.Trends
{
    public class Trend15Seconds : ITrend
    {
        public string Symbol { get; private set; }

        public int DataBasis { get; private set; }

        public decimal Seconds45 { get; private set; }

        public decimal Minutes1 { get; private set; }

        public decimal Minutes2 { get; private set; }

        public decimal Minutes3 { get; private set; }

        public Trend15Seconds(string symbol, int dataBasis, decimal seconds45, decimal minutes1, decimal minutes2, decimal minutes3)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Seconds45 = seconds45;
            this.Minutes1 = minutes1;
            this.Minutes2 = minutes2;
            this.Minutes3 = minutes3;
        }

        public bool IsValid()
        {
            // Roughly 3 * 60 (3 Minutes)
            return this.DataBasis > 170;
        }
    }
}
