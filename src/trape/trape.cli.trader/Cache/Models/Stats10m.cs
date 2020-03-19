namespace trape.cli.trader.Cache.Models
{
    public class Stats10m : ITrend
    {
        public string Symbol { get; private set; }

        public int DataBasis { get; private set; }

        public decimal Slope30m { get; private set; }

        public decimal Slope1h { get; private set; }

        public decimal Slope2h { get; private set; }

        public decimal Slope3h { get; private set; }

        public decimal MovingAverage30m { get; private set; }

        public decimal MovingAverage1h { get; private set; }

        public decimal MovingAverage2h { get; private set; }

        public decimal MovingAverage3h { get; private set; }

        public Stats10m(string symbol, int dataBasis, decimal slope30m, decimal slope1h, decimal slope2h, decimal slope3h,
            decimal movav30m, decimal movav1h, decimal movav2h, decimal movav3h)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Slope30m = slope30m;
            this.Slope1h = slope1h;
            this.Slope2h = slope2h;
            this.Slope3h = slope3h;
            this.MovingAverage30m = movav30m;
            this.MovingAverage1h = movav1h;
            this.MovingAverage2h = movav2h;
            this.MovingAverage3h = movav3h;
        }

        public bool IsValid()
        {
            // Roughly 3 * 60 * 60 (3 hours)
            return this.DataBasis > 10700;
        }
    }
}
