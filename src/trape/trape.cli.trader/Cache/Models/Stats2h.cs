namespace trape.cli.trader.Cache.Models
{
    public class Stats2h : ITrend
    {
        public string Symbol { get; private set; }

        public int DataBasis { get; private set; }

        public decimal Slope6h { get; private set; }

        public decimal Slope12h { get; private set; }

        public decimal Slope18h { get; private set; }

        public decimal Slope1d { get; private set; }

        public decimal MovingAverage6h { get; private set; }

        public decimal MovingAverage12h { get; private set; }

        public decimal MovingAverage18h { get; private set; }

        public decimal MovingAverage1d { get; private set; }

        public Stats2h(string symbol, int dataBasis, decimal slope6h, decimal slope12h, decimal slope18h, decimal slope1d,
            decimal movav6h, decimal movav12h, decimal movav18h, decimal movav1d)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Slope6h = slope6h;
            this.Slope12h = slope12h;
            this.Slope18h = slope18h;
            this.Slope1d = slope1d;
            this.MovingAverage6h = movav6h;
            this.MovingAverage12h = movav12h;
            this.MovingAverage18h = movav18h;
            this.MovingAverage1d = movav1d;
        }

        public bool IsValid()
        {
            // Roughly 24 * 60 * 60 (24 hours)
            return this.DataBasis > 86200;
        }
    }
}
