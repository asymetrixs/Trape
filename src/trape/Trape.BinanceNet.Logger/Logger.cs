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

        private ILogger _logger;

        #endregion

        #region Constructor

        public Logger(ILogger logger)
            : base()
        {
            this._logger = logger;
        }

        #endregion

        #region Methods

        public override Encoding Encoding => System.Text.Encoding.UTF8;

        public override void WriteLine(string value)
        {
            if (value == null)
            {
                return;
            }

            const string prefix = "BinanceClient";

            try
            {
                // Format is: $"{DateTime.Now:yyyy/MM/dd HH:mm:ss:fff} | {logType} | {message}";
                var parts = value.Split('|');

                // Split values so that they fit into Serilog
                var date = parts[0].Trim();
                var logType = parts[1].Trim();
                var message = parts[2].Trim();

                var nativeLogType = (LogVerbosity)Enum.Parse(typeof(LogVerbosity), logType);

                switch (nativeLogType)
                {
                    case LogVerbosity.Debug:
                        this._logger.Debug($"{prefix}: {message}");
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
                this._logger.Fatal($"{prefix}: {e.Message}");
                this._logger.Fatal($"{prefix}: Failed to log message: {value}");
            }
        }

        #endregion
    }
}
