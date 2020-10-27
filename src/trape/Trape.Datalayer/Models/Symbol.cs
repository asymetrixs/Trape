namespace trape.datalayer.Models
{
    /// <summary>
    /// Database class for symbol
    /// </summary>
    public class Symbol : AbstractKey
    {
        /// <summary>
        /// Symbol name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Is collection active or not
        /// </summary>
        public bool IsCollectionActive { get; set; }

        /// <summary>
        /// Is trading active or not
        /// </summary>
        public bool IsTradingActive { get; set; }
    }
}
