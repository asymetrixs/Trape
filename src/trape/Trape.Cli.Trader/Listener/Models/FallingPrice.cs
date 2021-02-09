using System;

namespace Trape.Cli.trader.Listener.Models
{
    /// <summary>
    /// This class holds the first time the price started dropping
    /// </summary>
    public class FallingPrice
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>FallingPrice</c> class.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="from"></param>
        /// <param name="since"></param>
        public FallingPrice(string symbol, decimal from, DateTime since)
        {
            #region Argument checks

            if (string.IsNullOrEmpty(symbol))
            {
                throw new ArgumentNullException(paramName: nameof(symbol));
            }

            if (from == default)
            {
                throw new ArgumentOutOfRangeException(paramName: nameof(from));
            }

            if (since == default)
            {
                throw new ArgumentOutOfRangeException(paramName: nameof(since));
            }

            #endregion

            Symbol = symbol;
            OriginalPrice = from;
            Since = since;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Last high price when falling started
        /// </summary>
        public decimal OriginalPrice { get; }

        /// <summary>
        /// Falling since
        /// </summary>
        public DateTime Since { get; }

        #endregion
    }
}
