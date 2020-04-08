namespace trape.cli.trader.Cache.Models
{
    /// <summary>
    /// Interface for trends
    /// </summary>
    public interface ITrend
    {
        /// <summary>
        /// If true, then the data basis is valid
        /// </summary>
        /// <returns></returns>
        bool IsValid();
    }
}
