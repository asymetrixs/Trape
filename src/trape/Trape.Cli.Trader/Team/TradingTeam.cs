using Binance.Net.Objects.Spot.MarketData;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Trape.Cli.trader.Analyze;
using Trape.Cli.trader.Listener;
using Trape.Cli.trader.Trading;

namespace Trape.Cli.trader.Team
{
    /// <summary>
    /// Starts, stops, and manages the <c>Brokers</c>
    /// </summary>
    public class TradingTeam : ITradingTeam
    {
        #region Fields

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

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
        /// Last active
        /// </summary>
        public DateTime LastActive { get; private set; }

        /// <summary>
        /// Subscriptions to subjects
        /// </summary>
        private IDisposable _listenerSubscription = Disposable.Empty;

        #endregion

        #region Constructor 

        /// <summary>
        /// Initializes a new instance of the <c>TradingTeam</c> class.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="buffer"></param>
        public TradingTeam(ILogger logger, IListener buffer)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            _listener = buffer ?? throw new ArgumentNullException(paramName: nameof(buffer));

            #endregion

            _logger = logger.ForContext<TradingTeam>();
            _team = new List<IStartable>();
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Spawns an analyst to listen for that symbol
        /// </summary>
        private async void SpawnAnalyst(BinanceSymbol binanceSymbol)
        {
            LastActive = DateTime.UtcNow;

            _logger.Verbose("Checking trading team...");


            if (_team.Any(t => t.BaseAsset == binanceSymbol.BaseAsset))
            {
                _logger.Verbose($"Team for {binanceSymbol.BaseAsset} is already spawned...");
                return;
            }

            // Instantiate pair of broker and analyst
            var broker = Program.Container.GetInstance<IBroker>();
            var analyst = Program.Container.GetInstance<IAnalyst>();

            _logger.Information($"{binanceSymbol.BaseAsset}: Adding Broker to the trading team.");
            _team.Add(broker);

            _logger.Information($"{binanceSymbol.BaseAsset}: Adding Analyst to the trading team.");
            _team.Add(analyst);

            var analystStart = analyst.Start(binanceSymbol);
            var brokerStart = broker.Start(binanceSymbol);
            
            await Task.WhenAll(new Task[] { analystStart, brokerStart }).ConfigureAwait(true);

            broker.SubscribeTo(analyst);

            // TODO: Faulty check

            _logger.Information($"Trading Team checked: {_team.Count} Member online"); ;
        }

        #endregion

        #region Start / Stop

        /// <summary>
        /// Start
        /// </summary>
        public void Start()
        {
            _logger.Debug("Starting trading team");

            _listenerSubscription = _listener.NewAssets.Subscribe(SpawnAnalyst);

            _logger.Debug("Trading team started");
        }

        /// <summary>
        /// Terminate
        /// </summary>
        /// <returns></returns>
        public void Terminate()
        {
            _logger.Debug("Stopping trading team");

            Parallel.ForEach(_team, async (team) => await team.Terminate().ConfigureAwait(false));

            _logger.Debug("Trading team stopped");
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var member in _team)
                {
                    member.Dispose();
                }

                _listenerSubscription.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
