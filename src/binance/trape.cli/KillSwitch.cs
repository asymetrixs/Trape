using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace trape.cli.collector
{
    public class KillSwitch : IKillSwitch
    {
        public CancellationToken CancellationToken { get; }
        
        public KillSwitch(CancellationToken cancellationToken)
        {
            if(null == cancellationToken)
            {
                throw new ArgumentNullException("CancellationToken cannot be null");
            }

            this.CancellationToken = cancellationToken;
        }
    }
}
