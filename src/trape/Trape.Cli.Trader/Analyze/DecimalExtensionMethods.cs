namespace Trape.Cli.trader.Analyze
{
    /// <summary>
    /// Class for decimal extension methods
    /// </summary>
    public static class DecimalExtensionMethods
    {
        /// <summary>
        /// Returns a part of the value multiplied by <paramref name="multiplyer"/> and adjusted to the values size.
        /// </summary>
        /// <param name="value">Value to be multiplied.</param>
        /// <param name="multiplyer">Value which X part is used for multiplication.</param>
        /// <returns>X part of <paramref name="multiplyer"/> times <paramref name="value"/>.</returns>
        public static decimal XPartOf(this decimal value, decimal multiplyer)
        {
            var correction = 1;
            if (value < 500)
            {
                correction = 10;
            }

            return value * (multiplyer * correction / 10000);
        }
    }
}
