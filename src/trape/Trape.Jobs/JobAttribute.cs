namespace Trape.Jobs
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class JobAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <c>JobAttribute</c> class.
        /// </summary>
        /// <param name="interval">Time Span</param>
        public JobAttribute(TimeSpan interval)
        {
            this.Interval = interval;
        }

        /// <summary>
        /// Interval
        /// </summary>
        public TimeSpan Interval { get; }
    }
}
