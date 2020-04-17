namespace trape.cli.trader.Analyze
{
    /// <summary>
    /// Recommendation for the Trader on how to act
    /// </summary>
    public enum Action
    {
        /// <summary>
        /// Hold
        /// </summary>
        Hold,
        /// <summary>
        /// Buy
        /// </summary>
        Buy,
        /// <summary>
        /// Sell
        /// </summary>
        Sell,
        /// <summary>
        /// Corner case strongly advising to buy
        /// </summary>
        StrongBuy,
        /// <summary>
        /// Corner case strongly advising to sell
        /// </summary>
        StrongSell,
        /// <summary>
        /// Corner case - market drop
        /// </summary>
        PanicSell
    }
}
