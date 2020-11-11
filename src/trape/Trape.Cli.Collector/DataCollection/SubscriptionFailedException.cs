using System;

namespace Trape.Cli.collector.DataCollection
{
    /// <summary>
    /// Is used in case of Binance Client cannot subscribe to symbols
    /// </summary>
    public class SubscriptionFailedException : Exception
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>SubscriptionFailedException</c> class.
        /// </summary>
        public SubscriptionFailedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>SubscriptionFailedException</c> class.
        /// </summary>
        /// <param name="message"></param>
        public SubscriptionFailedException(string message)
            : base(message)
        {

        }


        /// <summary>
        /// Initializes a new instance of the <c>SubscriptionFailedException</c> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>

        public SubscriptionFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion
    }
}
