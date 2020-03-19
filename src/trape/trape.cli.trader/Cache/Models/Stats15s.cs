namespace trape.cli.trader.Cache.Models
{
    public class Stats15s : ITrend
    {
        public string Symbol { get; private set; }

        public int DataBasis { get; private set; }

        public decimal Slope45s { get; private set; }

        public decimal Slope1m { get; private set; }

        public decimal Slope2m { get; private set; }

        public decimal Slope3m { get; private set; }

        public decimal MovingAverage45s { get; private set; }

        public decimal MovingAverage1m { get; private set; }

        public decimal MovingAverage2m { get; private set; }

        public decimal MovingAverage3m { get; private set; }

        public Stats15s(string symbol, int dataBasis, decimal slope45s, decimal slope1m, decimal slope2m, decimal slope3m,
            decimal movav45s, decimal movav1m, decimal movav2m, decimal movav3m)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Slope45s = slope45s;
            this.Slope1m = slope1m;
            this.Slope2m = slope2m;
            this.Slope3m = slope3m;
            this.MovingAverage45s = movav45s;
            this.MovingAverage1m = movav1m;
            this.MovingAverage2m = movav2m;
            this.MovingAverage3m = movav3m;
        }

        public bool IsValid()
        {
            // Roughly 3 * 60 (3 Minutes)
            return this.DataBasis > 170;
        }
    }
}
