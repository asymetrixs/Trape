using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace trape.cli.collector
{
    public interface IKillSwitch
    {
        CancellationToken CancellationToken { get; }
    }
}
