namespace Trape.Datalayer.Enums
{
    public enum TimeInForce
    {
        /// <summary>
        /// GoodTillCancel orders will stay active until they are filled or canceled
        /// </summary>
        GoodTillCancel = 0,

        /// <summary>
        /// ImmediateOrCancel orders have to be at least partially filled upon placing or
        /// will be automatically canceled
        /// </summary>
        ImmediateOrCancel = 1,

        /// <summary>
        /// FillOrKill orders have to be entirely filled upon placing or will be automatically
        /// canceled
        /// </summary>
        FillOrKill = 2
    }
}
