namespace Trape.Cli.Trader.Fees
{
    /// <summary>
    /// Interface for <c>FeeWatchdog</c>
    /// </summary>
    public interface IFeeWatchdog
    {
        /// <summary>
        /// Start
        /// </summary>
        void Start();

        /// <summary>
        /// Terminate
        /// </summary>
        void Terminate();
    }
}
