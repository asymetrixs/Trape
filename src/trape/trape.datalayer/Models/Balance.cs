using System;

namespace trape.datalayer.Models
{
    public class Balance : AbstractKey
    {
        /// <summary>
        /// The asset this balance is for
        /// </summary>
        public string Asset { get; set; }

        /// <summary>
        /// The amount that isn't locked in a trade
        /// </summary>
        public decimal Free { get; set; }

        /// <summary>
        /// The amount that is currently locked in a trade
        /// </summary>
        public decimal Locked { get; set; }

        /// <summary>
        /// The total balance of this asset (Free + Locked)
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Time when this record was created
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Foreign Key
        /// </summary>
        public long AccountInfoId { get; set; }

        /// <summary>
        /// Account Info
        /// </summary>
        public virtual AccountInfo AccountInfo { get; set; }
    }
}
