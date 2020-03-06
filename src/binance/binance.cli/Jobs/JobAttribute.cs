using System;

namespace binance.cli.Jobs
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class JobAttribute : Attribute
    {
        public TimeSpan Interval { get; private set; }

        public JobAttribute(int hours, int minutes, int seconds)
        {
            this.Interval = new TimeSpan(hours, minutes, seconds);
        }
    }
}
