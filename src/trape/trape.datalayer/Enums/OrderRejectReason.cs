namespace trape.datalayer.Enums
{
    public enum OrderRejectReason
    {
        /// <summary>
        /// Not rejected
        /// </summary>
        None = 0,

        /// <summary>
        /// Unknown instrument
        /// </summary>
        UnknownInstrument = 1,

        /// <summary>
        /// Closed market
        /// </summary>
        MarketClosed = 2,

        /// <summary>
        /// Quantity out of bounds
        /// </summary>
        PriceQuantityExceedsHardLimits = 3,

        /// <summary>
        /// Unknown order
        /// </summary>
        UnknownOrder = 4,

        /// <summary>
        /// Duplicate
        /// </summary>
        DuplicateOrder = 5,

        /// <summary>
        /// Unkown account
        /// </summary>
        UnknownAccount = 6,

        /// <summary>
        /// Not enough balance
        /// </summary>
        InsufficientBalance = 7,

        /// <summary>
        /// Account not active
        /// </summary>
        AccountInactive = 8,

        /// <summary>
        /// Cannot settle
        /// </summary>
        AccountCannotSettle = 9
    }
}