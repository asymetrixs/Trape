using Binance.Net.Objects.Spot.MarketStream;
using System;

namespace Trape.Cli.Trader.Team.Models
{
    public class CurrentBookPrice
    {
        /// <summary>
        /// Binance book price
        /// </summary>
        private readonly BinanceStreamBookPrice _binanceStreamBookPrice;

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentBookPrice"/> class.
        /// </summary>
        /// <param name="bsbp"></param>
        public CurrentBookPrice(BinanceStreamBookPrice bsbp)
        {
            this._binanceStreamBookPrice = bsbp;
            this.On = DateTime.Now;
        }

        /// <summary>
        /// Added onr
        /// </summary>
        public DateTime On { get; }

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol => _binanceStreamBookPrice.Symbol;

        /// <summary>
        /// Best Ask Price
        /// </summary>
        public decimal BestAskPrice => _binanceStreamBookPrice.BestAskPrice;

        /// <summary>
        /// Best Ask Quantity
        /// </summary>
        public decimal BestAskQuantity => _binanceStreamBookPrice.BestAskQuantity;

        /// <summary>
        /// Best Bid Price
        /// </summary>
        public decimal BestBidPrice => _binanceStreamBookPrice.BestBidPrice;

        /// <summary>
        /// Best Bid Quantity
        /// </summary>
        public decimal BestBidQuantity => _binanceStreamBookPrice.BestBidQuantity;

        /// <summary>
        /// Update Id
        /// </summary>
        public long UpdateId => _binanceStreamBookPrice.UpdateId;
    }
}
