using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using trape.datalayer.Models;
using Action = trape.datalayer.Enums.Action;

namespace trape.cli.trader.Analyze.Models
{
    /// <summary>
    /// <c>Analysis</c> produced by <c>Analyst</c>.
    /// </summary>
    public class Analysis
    {
        #region Fields

        /// <summary>
        /// Records last time an action occured
        /// </summary>
        private List<LastDecision> _lastDecisionTimes;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Analysis</c> class.
        /// </summary>
        /// <param name="symbol"></param>
        public Analysis(string symbol, IEnumerable<LastDecision> lastDecisions)
        {
            this.Symbol = symbol;
            this.Now = DateTime.UtcNow;
            this.IssuedBuyAfterLastPanicSell = default;
            this.LastPanicModeDetected = default;
            this.LastPanicModeEnded = default;
            this._lastDecisionTimes = lastDecisions.ToList();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Last update
        /// </summary>
        public DateTime Now { get; private set; }

        /// <summary>
        /// Symbol
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// Action
        /// </summary>
        public Action Action { get; private set; }

        /// <summary>
        /// Last race start
        /// </summary>
        public DateTime LastRaceDetected { get; private set; }

        /// <summary>
        /// Last race end
        /// </summary>
        public DateTime LastRaceEnded { get; private set; }

        /// <summary>
        /// Last jump start
        /// </summary>
        public DateTime LastJumpStart { get; private set; }

        /// <summary>
        /// Last jump end
        /// </summary>
        public DateTime LastJumpEnded { get; private set; }

        /// <summary>
        /// Last Panic Mode
        /// </summary>
        public DateTime LastPanicModeEnded { get; private set; }

        /// <summary>
        /// Last Panic Mode
        /// </summary>
        public DateTime LastPanicModeDetected { get; private set; }

        /// <summary>
        /// Indicates when last after PanicSell-buy was done
        /// </summary>
        public DateTime IssuedBuyAfterLastPanicSell { get; private set; }

        /// <summary>
        /// Indicates if the PanicSell has ended
        /// </summary>
        /// <returns></returns>
        public bool PanicSellHasEnded => this.LastPanicModeEnded.AddMinutes(15) < this.Now;

        #endregion

        #region Methods

        /// <summary>
        /// Updates when the given action appeared last
        /// </summary>
        /// <param name="action">Action</param>        
        public void UpdateAction(Action action)
        {
            var decision = this._lastDecisionTimes.FirstOrDefault(l => l.Action == Action);
            if (decision == null)
            {
                this._lastDecisionTimes.Add(new LastDecision() { Action = action, Symbol = this.Symbol, EventTime = DateTime.UtcNow });
            }
            else
            {
                decision.EventTime = DateTime.UtcNow;
            }

            this.Action = action;
        }

        /// <summary>
        /// Call first to set time correctly
        /// </summary>
        public void PrepareForUpdate()
        {
            this.Now = DateTime.UtcNow;
        }

        /// <summary>
        /// Race detected
        /// </summary>
        public void RaceDetected()
        {
            if (this.LastRaceEnded < this.Now.AddSeconds(-5))
            {
                this.LastRaceDetected = this.Now;
            }
        }

        /// <summary>
        /// Race ended
        /// </summary>
        public void RaceEnded()
        {
            this.LastRaceEnded = this.Now;
        }

        /// <summary>
        /// Panic mode detected
        /// </summary>
        public void PanicDetected()
        {
            if (this.LastPanicModeEnded.AddSeconds(5) < this.Now)
            {
                this.LastPanicModeDetected = this.Now;
            }
            this.LastPanicModeEnded = this.Now;
        }

        /// <summary>
        /// Jump detected
        /// </summary>
        public void JumpDetected()
        {
            if (this.LastJumpStart < this.Now.AddSeconds(-10))
            {
                this.LastJumpStart = this.Now;
            }

            this.LastJumpEnded = this.Now;
        }

        /// <summary>
        /// Returns date when action occured last
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DateTime GetLastDateOf(Action action)
        {
            var decision = this._lastDecisionTimes.FirstOrDefault(l => l.Action == action);
            if (decision != null)
            {
                return decision.EventTime;
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Records that a buy was issued after last Panic Sell
        /// </summary>
        public bool BuyAfterPanicSell()
        {
            // If last buy does not match end date, no buy has been issued yet
            if (this.IssuedBuyAfterLastPanicSell != this.LastPanicModeEnded)
            {
                this.IssuedBuyAfterLastPanicSell = this.LastPanicModeEnded;
                return true;
            }

            return false;
        }

        #endregion
    }
}
