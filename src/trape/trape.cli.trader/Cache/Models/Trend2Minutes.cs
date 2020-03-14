namespace trape.cli.trader.Cache.Models
{
    public class Trend2Minutes : ITrend
    {
        public string Symbol { get; private set; }

        public int DataBasis { get; private set; }

        public decimal Minutes5 { get; private set; }

        public decimal Minutes7 { get; private set; }

        public decimal Minutes10 { get; private set; }

        public decimal Minutes15 { get; private set; }

        public Trend2Minutes(string symbol, int dataBasis, decimal minutes5, decimal minutes7, decimal minutes10, decimal minutes15)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Minutes5 = minutes5;
            this.Minutes7 = minutes7;
            this.Minutes10 = minutes10;
            this.Minutes15 = minutes15;
        }

        public bool IsValid()
        {
            // Roughly 15 * 60 (15 Minutes)
            return this.DataBasis > 858;
        }
    }
}
