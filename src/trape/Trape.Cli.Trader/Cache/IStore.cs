namespace Trape.Cli.Trader.Cache
{
    using Trape.Cli.Trader.Cache.Models;

    public interface IStore
    {
        /// <summary>
        /// Removes an open order
        /// </summary>
        /// <param name="clientOrderId">Id of open order</param>
        void RemoveOpenOrder(string clientOrderId);

        /// <summary>
        /// Stores open orders
        /// </summary>
        /// <param name="openOrder">Open Order</param>
        void AddOpenOrder(OpenOrder openOrder);

        /// <summary>
        /// Returns the currently blocked amount
        /// </summary>
        decimal GetOpenOrderValue(string symbol);

        /// <summary>
        /// Returns whether an order for this symbol is open or not
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns></returns>
        bool HasOpenOrder(string symbol);
    }
}
