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
        private readonly List<LastDecision> _lastDecisionTimes;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Analysis</c> class.
        /// </summary>
        /// <param name="symbol"></param>
        public Analysis(string symbol, IEnumerable<LastDecision> lastDecisions)
        {
            Symbol = symbol;
            Now = DateTime.UtcNow;
            IssuedBuyAfterLastPanicSell = default;
            LastPanicModeDetected = default;
            LastPanicModeEnded = default;
            _lastDecisionTimes = lastDecisions.ToList();
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
        public bool PanicSellHasEnded => LastPanicModeEnded.AddMinutes(15) < Now;

        #endregion

        #region Methods

        /// <summary>
        /// Updates when the given action appeared last
        /// </summary>
        /// <param name="action">Action</param>        
        public void UpdateAction(Action action)
        {
            var decision = _lastDecisionTimes.FirstOrDefault(l => l.Action == Action);
            if (decision == null)
            {
                _lastDecisionTimes.Add(new LastDecision() { Action = action, Symbol = Symbol, EventTime = DateTime.UtcNow });
            }
            else
            {
                decision.EventTime = DateTime.UtcNow;
            }

            Action = action;
        }

        /// <summary>
        /// Call first to set time correctly
        /// </summary>
        public void PrepareForUpdate()
        {
            Now = DateTime.UtcNow;
        }

        /// <summary>
        /// Race detected
        /// </summary>
        public void RaceDetected()
        {
            if (LastRaceEnded < Now.AddSeconds(-5))
            {
                LastRaceDetected = Now;
            }
        }

        /// <summary>
        /// Race ended
        /// </summary>
        public void RaceEnded()
        {
            LastRaceEnded = Now;
        }

        /// <summary>
        /// Panic mode detected
        /// </summary>
        public void PanicDetected()
        {
            if (LastPanicModeEnded.AddSeconds(5) < Now)
            {
                LastPanicModeDetected = Now;
            }
            LastPanicModeEnded = Now;
        }

        /// <summary>
        /// Jump detected
        /// </summary>
        public void JumpDetected()
        {
            if (LastJumpStart < Now.AddSeconds(-10))
            {
                LastJumpStart = Now;
            }

            LastJumpEnded = Now;
        }

        /// <summary>
        /// Returns date when action occured last
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DateTime GetLastDateOf(Action action)
        {
            var decision = _lastDecisionTimes.FirstOrDefault(l => l.Action == action);
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
            if (IssuedBuyAfterLastPanicSell != LastPanicModeEnded)
            {
                IssuedBuyAfterLastPanicSell = LastPanicModeEnded;
                return true;
            }

            return false;
        }

        #endregion
    }
}
