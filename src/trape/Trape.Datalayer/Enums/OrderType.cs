namespace Trape.Datalayer.Enums
{
    public enum OrderType
    {
        /// <summary>
        /// Limit orders will be placed at a specific price. If the price isn't available
        /// in the order book for that asset the order will be added in the order book for
        /// someone to fill.
        /// </summary>
        Limit = 0,

        /// <summary>
        /// Market order will be placed without a price. The order will be executed at the
        /// best price available at that time in the order book.
        /// </summary>
        Market = 1,

        /// <summary>
        /// Stop loss order. Will execute a market order when the price drops below a price
        /// to sell and therefor limit the loss
        /// </summary>
        StopLoss = 2,

        /// <summary>
        /// Stop loss order. Will execute a limit order when the price drops below a price
        /// to sell and therefor limit the loss
        /// </summary>
        StopLossLimit = 3,

        /// <summary>
        /// Take profit order. Will execute a market order when the price rises above a price
        /// to sell and therefor take a profit
        /// </summary>
        TakeProfit = 4,

        /// <summary>
        /// Take profit order. Will execute a limit order when the price rises above a price
        /// to sell and therefor take a profit
        /// </summary>
        TakeProfitLimit = 5,

        /// <summary>
        /// Same as a limit order, however it will fail if the order would immediately match,
        /// therefor preventing taker orders
        /// </summary>
        LimitMaker = 6
    }
}
