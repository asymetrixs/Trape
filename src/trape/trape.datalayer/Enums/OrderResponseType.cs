namespace trape.datalayer.Enums
{
    public enum OrderResponseType
    {
        /// <summary>
        /// Ack only
        /// </summary>
        Acknowledge = 0,

        /// <summary>
        /// Resulting order
        /// </summary>
        Result = 1,

        /// <summary>
        /// Full order info
        /// </summary>
        Full = 2
    }
}
