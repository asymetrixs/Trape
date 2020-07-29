using System;

namespace trape.jobs
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class JobAttribute : Attribute
    {
        #region Properites

        /// <summary>
        /// Interval
        /// </summary>
        public TimeSpan Interval { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>JobAttribute</c> class.
        /// </summary>
        /// <param name="hours">Hours</param>
        /// <param name="minutes">Minutes</param>
        /// <param name="seconds">Seconds</param>
        public JobAttribute(int hours, int minutes, int seconds)
        {
            this.Interval = new TimeSpan(hours, minutes, seconds);
        }

        #endregion
    }
}
