namespace trape.cli.trader.Analyze
{
    public enum Strategy
    {
        /// <summary>
        /// No strategy is defined
        /// </summary>
        Hold,
        /// <summary>
        /// Market moves slightly upwards
        /// </summary>
        NormalSell,
        /// <summary>
        /// Market moves slightly downwards
        /// </summary>
        NormalBuy,
        /// <summary>
        /// Market moves upwards
        /// </summary>
        StrongSell,
        /// <summary>
        /// Market moves downwards
        /// </summary>
        StrongBuy,
        /// <summary>
        /// Market is crashing
        /// </summary>
        CrashingSell
    }
}
