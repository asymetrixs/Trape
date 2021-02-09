using CryptoExchange.Net.Logging;
using Serilog;
using System;
using System.IO;
using System.Text;

namespace Trape.BinanceNet.Logger
{
    public class Logger : TextWriter
    {
        #region Fields

        /// <summary>
        /// Instance of SeriLog
        /// </summary>
        private readonly ILogger _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>Logger</c> class.
        /// </summary>
        /// <param name="logger"></param>
        public Logger(ILogger logger)
            : base()
        {
            _logger = logger.ForContext(typeof(Binance.Net.BinanceClient));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Set encoding to UTF8
        /// </summary>
        public override Encoding Encoding => Encoding.UTF8;

        /// <summary>
        /// Overriding method used by Binance.NET
        /// </summary>
        /// <param name="value"></param>
        public override void WriteLine(string value)
        {
            if (value == null)
            {
                return;
            }

            const string prefix = "BinanceClient";

            try
            {
                // Format is: $"{DateTime.Now:yyyy/MM/dd HH:mm:ss:fff} | Binance | {logType} | {message}";
                var parts = value.Split('|');

                // Split values so that they fit into Serilog
                var date = parts[0].Trim();
                var logType = parts[2].Trim();
                var message = parts[3].Trim();

                var nativeLogType = (LogVerbosity)Enum.Parse(typeof(LogVerbosity), logType);

                switch (nativeLogType)
                {
                    case LogVerbosity.Debug:
                        _logger.Verbose($"{prefix}: {message}");
                        break;
                    case LogVerbosity.Info:
                        _logger.Information($"{prefix}: {message}");
                        break;
                    case LogVerbosity.Warning:
                        _logger.Warning($"{prefix}: {message}");
                        break;
                    case LogVerbosity.Error:
                        _logger.Error($"{prefix}: {message}");
                        break;
                    default:
                        // Just so that falls into the eye
                        _logger.Fatal($"{prefix}: {message}");
                        break;
                }
            }
            catch (Exception e)
            {
                if (value.Length > 30)
                {
                    value = value.Substring(0, 30) + "...";
                }

                if(e.Message.Contains("was not found", StringComparison.InvariantCulture))
                {
                    return;
                }

                _logger.Fatal($"{prefix}: {e.Message} - {value}");
            }
        }

        #endregion
    }
}
