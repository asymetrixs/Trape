namespace trape.cli.trader.Cache.Trends
{
    public class Trend2Hours : ITrend
    {
        public string Symbol { get; private set; }

        public int DataBasis { get; private set; }

        public decimal Hours6 { get; private set; }

        public decimal Hours12 { get; private set; }

        public decimal Hours18 { get; private set; }

        public decimal Day1 { get; private set; }

        public Trend2Hours(string symbol, int dataBasis, decimal hours6, decimal hours12, decimal hours18, decimal day1)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Hours6 = hours6;
            this.Hours12 = hours12;
            this.Hours18 = hours18;
            this.Day1 = day1;
        }

        public bool IsValid()
        {
            // Roughly 24 * 60 * 60 (24 hours)
            return this.DataBasis > 86300;
        }
    }
}
