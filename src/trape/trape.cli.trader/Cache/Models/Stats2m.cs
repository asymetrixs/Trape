namespace trape.cli.trader.Cache.Models
{
    public class Stats2m : ITrend
    {
        public string Symbol { get; private set; }

        public int DataBasis { get; private set; }

        public decimal Slope5m { get; private set; }

        public decimal Slope7m { get; private set; }

        public decimal Slope10m { get; private set; }

        public decimal Slope15m { get; private set; }

        public decimal MovingAverage5m { get; private set; }

        public decimal MovingAverage7m { get; private set; }

        public decimal MovingAverage10m { get; private set; }

        public decimal MovingAverage15m { get; private set; }

        public Stats2m(string symbol, int dataBasis, decimal slope5m, decimal slope7m, decimal slope10m, decimal slope15m,
            decimal movav5m, decimal movav7m, decimal movav10m, decimal movav15m)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Slope5m = slope5m;
            this.Slope7m = slope7m;
            this.Slope10m = slope10m;
            this.Slope15m = slope15m;
            this.MovingAverage5m = movav5m;
            this.MovingAverage7m = movav7m;
            this.MovingAverage10m = movav10m;
            this.MovingAverage15m = movav15m;
        }

        public bool IsValid()
        {
            // Roughly 15 * 60 (15 Minutes)
            return this.DataBasis > 858;
        }
    }
}
