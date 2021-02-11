namespace Trape.Cli.Trader.Team.Models
{
    using Binance.Net.Objects.Spot.MarketStream;
    using System;

    public class CurrentBookPrice
    {
        /// <summary>
        /// Binance book price
        /// </summary>
        private readonly BinanceStreamBookPrice _binanceStreamBookPrice;

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentBookPrice"/> class.
        /// </summary>
        /// <param name="bsbp">Binance Book Price</param>
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
        public string Symbol => this._binanceStreamBookPrice.Symbol;

        /// <summary>
        /// Best Ask Price
        /// </summary>
        public decimal BestAskPrice => this._binanceStreamBookPrice.BestAskPrice;

        /// <summary>
        /// Best Ask Quantity
        /// </summary>
        public decimal BestAskQuantity => this._binanceStreamBookPrice.BestAskQuantity;

        /// <summary>
        /// Best Bid Price
        /// </summary>
        public decimal BestBidPrice => this._binanceStreamBookPrice.BestBidPrice;

        /// <summary>
        /// Best Bid Quantity
        /// </summary>
        public decimal BestBidQuantity => this._binanceStreamBookPrice.BestBidQuantity;

        /// <summary>
        /// Update Id
        /// </summary>
        public long UpdateId => this._binanceStreamBookPrice.UpdateId;
    }
}
