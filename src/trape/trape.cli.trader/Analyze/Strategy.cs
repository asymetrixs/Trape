namespace trape.cli.trader.Analyze
{
    public enum Strategy
    {
        /// <summary>
        /// No strategy is defined
        /// </summary>
        None,
        /// <summary>
        /// Market moves slightly upwards
        /// </summary>
        HorizontalSell,
        /// <summary>
        /// Market moves slightly downwards
        /// </summary>
        HorizontalBuy,
        /// <summary>
        /// Market moves upwards
        /// </summary>
        VerticalSell,
        /// <summary>
        /// Market moves downwards
        /// </summary>
        VerticalBuy,
        /// <summary>
        /// Market is crashing
        /// </summary>
        CrashingSell
    }
}
