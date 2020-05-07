using System;

namespace trape.datalayer.Models
{
    public class BalanceUpdate : AbstractKey
    {
        /// <summary>
        /// The asset which changed
        /// </summary>
        public string Asset { get; set; }

        /// <summary>
        /// The balance delta
        /// </summary>
        public decimal BalanceDelta { get; set; }

        /// <summary>
        /// The time the deposit/withdrawal was cleared
        /// </summary>
        public DateTime ClearTime { get; set; }
    }
}
