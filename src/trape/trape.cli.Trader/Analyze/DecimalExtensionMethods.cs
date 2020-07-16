namespace trape.cli.trader.Analyze
{
    /// <summary>
    /// Class for decimal extension methods
    /// </summary>
    public static class DecimalExtensionMethods
    {
        /// <summary>
        /// Multiplies the current value times the ten thousandth of <paramref name="multiplyer"/>
        /// </summary>
        /// <param name="value">Value to be multiplied.</param>
        /// <param name="multiplyer">Value which tenthousandth part is used for multiplication.</param>
        /// <returns>Tenthousandth <paramref name="multiplyer"/> times <paramref name="value"/>.</returns>
        public static decimal TenThousandth(this decimal value, decimal multiplyer)
        {
            return value * (multiplyer / 10000);
        }
    }
}
