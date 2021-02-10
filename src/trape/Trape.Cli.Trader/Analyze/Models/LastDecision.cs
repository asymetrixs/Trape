using System;

namespace Trape.Cli.Trader.Analyze.Models
{
    public class LastDecision
    {
        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        public ActionType Action { get; set; }

        /// <summary>
        /// Last occurence
        /// </summary>
        public DateTime EventTime { get; set; }

        #endregion
    }
}
