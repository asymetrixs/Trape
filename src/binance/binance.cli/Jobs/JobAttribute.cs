using System;

namespace binance.cli.Jobs
{
    class JobAttribute : Attribute
    {
        public TimeSpan Interval { get; private set; }

        public JobAttribute(int hours, int minutes, int seconds)
        {
            this.Interval = new TimeSpan(hours, minutes, seconds);
        }
    }
}
