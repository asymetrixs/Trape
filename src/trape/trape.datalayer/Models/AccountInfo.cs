using System;
using System.Collections.Generic;
using System.Text;

namespace trape.datalayer.Models
{
    public class AccountInfo : AbstractKey
    {
        /// <summary>
        /// Last Update
        /// </summary>
        public DateTime UpdatedOn { get; set; }

        /// <summary>
        /// Commission percentage to pay when making trades
        /// </summary>
        public decimal MakerCommission { get; set; }

        /// <summary>
        /// Commission percentage to pay when taking trades
        /// </summary>
        public decimal TakerCommission { get; set; }

        /// <summary>
        /// Commission percentage to buy when buying
        /// </summary>
        public decimal BuyerCommission { get; set; }

        /// <summary>
        /// Commission percentage to buy when selling
        /// </summary>
        public decimal SellerCommission { get; set; }

        /// <summary>
        /// Boolean indicating if this account can trade
        /// </summary>
        public bool CanTrade { get; set; }

        /// <summary>
        /// Boolean indicating if this account can withdraw
        /// </summary>
        public bool CanWithdraw { get; set; }

        /// <summary>
        /// Boolean indicating if this account can deposit
        /// </summary>
        public bool CanDeposit { get; set; }

        /// <summary>
        /// List of assets with their current balances
        /// </summary>
        public virtual List<Balance> Balances { get; set; }
    }
}
