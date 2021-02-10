using Trape.Cli.trader.Cache.Models;

namespace Trape.Cli.Trader.Cache
{
    public interface ICache
    {

        /// <summary>
        /// Removes an open order
        /// </summary>
        /// <param name="clientOrderId">Id of open order</param>
        void RemoveOpenOrder(string clientOrderId);

        /// <summary>
        /// Stores open orders
        /// </summary>
        /// <param name="openOrder"></param>
        void AddOpenOrder(OpenOrder openOrder);

        /// <summary>
        /// Returns the currently blocked amount
        /// </summary>
        decimal GetOpenOrderValue(string symbol);

        /// <summary>
        /// Returns whether an order for this symbol is open or not
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        bool HasOpenOrder(string symbol);
    }
}
