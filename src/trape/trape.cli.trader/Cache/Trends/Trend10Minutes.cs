namespace trape.cli.trader.Cache.Trends
{
    public class Trend10Minutes : ITrend
    {
        public string Symbol { get; private set; }

        public int DataBasis { get; private set; }

        public decimal Minutes30 { get; private set; }

        public decimal Hours1 { get; private set; }

        public decimal Hours2 { get; private set; }

        public decimal Hours3 { get; private set; }

        public Trend10Minutes(string symbol, int dataBasis, decimal minutes30, decimal hours1, decimal hours2, decimal hours3)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Minutes30 = minutes30;
            this.Hours1 = hours1;
            this.Hours2 = hours2;
            this.Hours3 = hours3;
        }

        public bool IsValid()
        {
            // Roughly 3 * 60 * 60 (3 hours)
            return this.DataBasis > 10700;
        }
    }
}
