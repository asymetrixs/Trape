using System;
using System.ComponentModel.DataAnnotations.Schema;
using Action = trape.datalayer.Enums.Action;

namespace trape.datalayer.Models
{
    /// <summary>
    /// Holds information about when a decision was taken for the last time
    /// </summary>
    public class LastDecision
    {
        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        [Column("r_symbol")]
        public string Symbol { get; set; }

        /// <summary>
        /// Decision
        /// </summary>
        [Column("r_decision", TypeName = "text")]
        public Action Decision { get; set; }

        /// <summary>
        /// Last occurence
        /// </summary>
        [Column("r_event_time")]
        public DateTime EventTime { get; set; }

        #endregion
    }
}
