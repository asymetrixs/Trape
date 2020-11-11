namespace Trape.Datalayer.Enums
{
    public enum OrderStatus
    {
        /// <summary>
        /// Order is new
        /// </summary>
        New = 0,

        /// <summary>
        /// Order is partly filled, still has quantity left to fill
        /// </summary>
        PartiallyFilled = 1,

        /// <summary>
        /// The order has been filled and completed
        /// </summary>
        Filled = 2,

        /// <summary>
        /// The order has been canceled
        /// </summary>
        Canceled = 3,

        /// <summary>
        /// The order is in the process of being canceled
        /// </summary>
        PendingCancel = 4,

        //The order has been rejected
        Rejected = 5,

        /// <summary>
        /// The order has expired
        /// </summary>
        Expired = 6
    }
}
