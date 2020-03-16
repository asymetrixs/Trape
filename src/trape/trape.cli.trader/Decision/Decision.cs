namespace trape.cli.trader.Decision
{
    public class Decision
    {
        public string Symbol { get; set; }

        public decimal Price { get; set; }

        public Action Action { get; set; }

        public decimal Indicator { get; set; }
    }
}
