namespace trape.cli.trader.Cache.Models
{
    public class Stats3s : ITrend
    {
        public string Symbol { get; private set; }

        public int DataBasis { get; private set; }

        public decimal Slope5s { get; private set; }

        public decimal Slope10s { get; private set; }

        public decimal Slope15s { get; private set; }

        public decimal Slope30s { get; private set; }

        public decimal MovingAverage5s { get; private set; }

        public decimal MovingAverage10s { get; private set; }

        public decimal MovingAverage15s { get; private set; }

        public decimal MovingAverage30s { get; private set; }

        public Stats3s(string symbol, int dataBasis, decimal slope5s, decimal slope10s, decimal slope15s, decimal slope30s,
            decimal movav5s, decimal movav10s, decimal movav15s, decimal movav30s)
        {
            this.Symbol = symbol;
            this.DataBasis = dataBasis;
            this.Slope5s = slope5s;
            this.Slope10s = slope10s;
            this.Slope15s = slope15s;
            this.Slope30s = slope30s;
            this.MovingAverage5s = movav5s;
            this.MovingAverage10s = movav10s;
            this.MovingAverage15s = movav15s;
            this.MovingAverage30s = movav30s;
        }

        public bool IsValid()
        {
            // Roughly 30 (30 seconds)
            return this.DataBasis > 25;
        }
    }
}
