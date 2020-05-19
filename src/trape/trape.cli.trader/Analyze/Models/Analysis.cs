using System;
using System.Collections.Generic;
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
        private Dictionary<Action, DateTime> _lastActionTime;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Analysis</c> class.
        /// </summary>
        /// <param name="symbol"></param>
        public Analysis(string symbol)
        {
            this.Symbol = symbol;
            this._lastActionTime = new Dictionary<Action, DateTime>();
            this.Now = DateTime.UtcNow;
            this.IssuedBuyAfterLastPanicSell = default;
            this.LastPanicModeDetected = default;
            this.LastPanicModeEnded = default;
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
            if (!this._lastActionTime.ContainsKey(action))
            {
                this._lastActionTime.Add(action, this.Now);
            }

            this._lastActionTime[action] = this.Now;
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
            if (this._lastActionTime.ContainsKey(action))
            {
                return this._lastActionTime[action];
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
