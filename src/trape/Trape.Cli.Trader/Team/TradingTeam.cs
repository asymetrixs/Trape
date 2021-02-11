namespace Trape.Cli.Trader.Team
{
    using Binance.Net.Objects.Spot.MarketData;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Threading.Tasks;
    using Trape.Cli.Trader.Listener;

    /// <summary>
    /// Starts, stops, and manages the <c>Brokers</c>
    /// </summary>
    public class TradingTeam : ITradingTeam
    {
        /// <summary>
        /// Logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Team holding the <c>Broker</c>s
        /// </summary>
        private readonly List<IStartable> _team;

        /// <summary>
        /// Listener
        /// </summary>
        private readonly IListener _listener;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Subscriptions to subjects
        /// </summary>
        private IDisposable _listenerSubscription = Disposable.Empty;

        /// <summary>
        /// Initializes a new instance of the <c>TradingTeam</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="listener">Listener</param>
        public TradingTeam(ILogger logger, IListener listener)
        {
            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            this._listener = listener ?? throw new ArgumentNullException(paramName: nameof(listener));

            this._logger = logger.ForContext<TradingTeam>();
            this._team = new List<IStartable>();
        }

        /// <summary>
        /// Last active
        /// </summary>
        public DateTime LastActive { get; private set; }

        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            this._logger.Debug("Starting trading team");

            this._listenerSubscription = this._listener.NewAssets.Subscribe(this.SpawnAnalyst);

            this._logger.Debug("Trading team started");
        }

        /// <summary>
        /// Terminate
        /// </summary>
        public void Terminate()
        {
            this._logger.Debug("Stopping trading team");

            Parallel.ForEach(this._team, async (team) => await team.Terminate().ConfigureAwait(false));

            this._logger.Debug("Trading team stopped");
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var member in this._team)
                {
                    member.Dispose();
                }

                this._listenerSubscription.Dispose();
            }

            this._disposed = true;
        }

        /// <summary>
        /// Spawns an analyst to listen for that symbol
        /// </summary>
        private async void SpawnAnalyst(BinanceSymbol binanceSymbol)
        {
            this.LastActive = DateTime.UtcNow;

            this._logger.Verbose("Checking trading team...");

            if (this._team.Any(t => t.BaseAsset == binanceSymbol.BaseAsset))
            {
                this._logger.Verbose($"Team for {binanceSymbol.BaseAsset} is already spawned...");
                return;
            }

            IAnalyst? analyst = null;
            IBroker? broker = null;

            // try 3 times to spawn
            for (int i = 0; i < 3; i++)
            {
                // Instantiate pair of broker and analyst
                broker = Program.Container.GetInstance<IBroker>();
                analyst = Program.Container.GetInstance<IAnalyst>();

                var analystStart = analyst.Start(binanceSymbol);
                var brokerStart = broker.Start(binanceSymbol);

                await Task.WhenAll(new Task[] { analystStart, brokerStart }).ConfigureAwait(true);

                broker.SubscribeTo(analyst);

                if (broker.IsFaulty || analyst.IsFaulty)
                {
                    await Task.WhenAll(analyst.Terminate(), broker.Terminate()).ConfigureAwait(true);
                    broker.Dispose();
                    analyst.Dispose();

                    broker = null;
                    analyst = null;
                }
                else
                {
                    break;
                }
            }

            if (analyst is null || broker is null)
            {
                this._logger.Warning($"{binanceSymbol.BaseAsset}: Swaning analyst and broker failed.");
                return;
            }

            this._logger.Information($"{binanceSymbol.BaseAsset}: Adding Broker to the trading team.");
            this._team.Add(broker);

            this._logger.Information($"{binanceSymbol.BaseAsset}: Adding Analyst to the trading team.");
            this._team.Add(analyst);

            this._logger.Information($"Trading Team checked: {this._team.Count} Member online");
        }
    }
}
