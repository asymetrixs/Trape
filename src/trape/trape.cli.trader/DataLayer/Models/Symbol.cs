using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trape.cli.trader.DataLayer.Models
{
    /// <summary>
    /// Database class for symbol
    /// </summary>
    [Table("symbol")]
    public class Symbol
    {
        #region Properties

        /// <summary>
        /// Primary key
        /// </summary>
        [Key]
        [Column("id", TypeName = "int")]
        public int Id { get; set; }

        /// <summary>
        /// Symbol name
        /// </summary>
        [Column("name", TypeName = "text")]
        public string Name { get; set; }

        /// <summary>
        /// Is collection active or not
        /// </summary>
        [Column("is_collection_active", TypeName = "boolean")]
        public bool IsCollectionActive { get; set; }

        /// <summary>
        /// Is trading active or not
        /// </summary>
        [Column("is_trading_active", TypeName = "boolean")]
        public bool IsTradingActive { get; set; }

        #endregion
    }
}
