using System;
using System.ComponentModel.DataAnnotations.Schema;
using Action = trape.datalayer.Enums.Action;

namespace trape.datalayer.Models
{
    /// <summary>
    /// Holds information about when a decision was taken for the last time
    /// </summary>
    [Table("last_decisions", Schema = "stubs")]
    public class LastDecision
    {
        #region Properties

        /// <summary>
        /// Symbol
        /// </summary>
        [Column("r_symbol")]
        public string Symbol { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        [Column("r_action", TypeName = "int4")]
        public Action Action { get; set; }

        /// <summary>
        /// Last occurence
        /// </summary>
        [Column("r_event_time")]
        public DateTime EventTime { get; set; }

        #endregion
    }
}
