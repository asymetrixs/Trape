namespace Trape.BinanceNet.Logger
{
    using CryptoExchange.Net.Logging;
    using Serilog;
    using System;
    using System.IO;
    using System.Text;

    public class BinanceLoggerAdapter : TextWriter
    {
        /// <summary>
        /// Instance of SeriLog
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <c>Logger</c> class.
        /// </summary>
        /// <param name="logger">Logger</param>
        public BinanceLoggerAdapter(ILogger logger)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));

            this._logger = logger.ForContext<Binance.Net.BinanceClient>();
        }

        /// <summary>
        /// Set encoding to UTF8
        /// </summary>
        public override Encoding Encoding => Encoding.UTF8;

        /// <summary>
        /// Overriding method used by Binance.NET
        /// </summary>
        /// <param name="value">Value</param>
        public override void WriteLine(string? value)
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
                        this._logger.Verbose($"{prefix}: {message}");
                        break;

                    case LogVerbosity.Info:
                        this._logger.Information($"{prefix}: {message}");
                        break;

                    case LogVerbosity.Warning:
                        this._logger.Warning($"{prefix}: {message}");
                        break;

                    case LogVerbosity.Error:
                        this._logger.Error($"{prefix}: {message}");
                        break;

                    default:
                        // Just so that falls into the eye
                        this._logger.Fatal($"{prefix}: {message}");
                        break;
                }
            }
            catch (Exception e)
            {
                if (value.Length > 30)
                {
                    value = value.Substring(0, 30) + "...";
                }

                if (e.Message.Contains("was not found", StringComparison.InvariantCulture))
                {
                    return;
                }

                this._logger.Fatal($"{prefix}: {e.Message} - {value}");
            }
        }
    }
}
