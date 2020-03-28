using Serilog;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace trape.cli.trader
{
    public class BinanceLogger : TextWriter
    {
        private ILogger _logger;

        public BinanceLogger(ILogger logger)
            : base()
        {
            this._logger = logger;
        }

        public override Encoding Encoding => System.Text.Encoding.UTF8;

        public override void Write(bool value)
        {
            this._logger.Information(value.ToString());
        }

        public override void Write(char value)
        {
            this._logger.Information(value.ToString());
        }

        public override void Write(char[] buffer)
        {
            this._logger.Information(new string(buffer));
        }

        public override void Write(char[] buffer, int index, int count)
        {
            this._logger.Information(new string(buffer));
        }

        public override void Write(decimal value)
        {
            this._logger.Information(value.ToString());
        }

        public override void Write(double value)
        {
            this._logger.Information(value.ToString());
        }

        public override void Write(int value)
        {
            this._logger.Information(value.ToString());
        }

        public override void Write(long value)
        {
            this._logger.Information(value.ToString());
        }

        public override void Write(object value)
        {
            this._logger.Information(value.ToString());
        }

        public override void Write(ReadOnlySpan<char> buffer)
        {
            this._logger.Information(new string(buffer));
        }

        public override void Write(float value)
        {
            this._logger.Information(value.ToString());
        }

        public override void Write(string value)
        {
            this._logger.Information(value);
        }

        public override void Write(string format, object arg0)
        {
            this._logger.Information(string.Format(format, arg0));
        }

        public override void Write(string format, object arg0, object arg1)
        {
            this._logger.Information(string.Format(format, arg0, arg1));
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            this._logger.Information(string.Format(format, arg0, arg1, arg2));
        }

        public override void Write(string format, params object[] arg)
        {
            this._logger.Information(string.Format(format, arg));
        }

        public override void Write(StringBuilder value)
        {
            this._logger.Information(value.ToString());
        }

        public override void Write(uint value)
        {
            this._logger.Information(value.ToString());
        }

        public override void Write(ulong value)
        {
            this._logger.Information(value.ToString());
        }

        public override Task WriteAsync(char value)
        {
            this._logger.Information(value.ToString());
            return Task.CompletedTask;
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            this._logger.Information(new string(buffer));
            return Task.CompletedTask;
        }

        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("not implemented");
        }

        public override Task WriteAsync(string value)
        {
            this._logger.Information(value.ToString());
            return Task.CompletedTask;
        }

        public override Task WriteAsync(StringBuilder value, CancellationToken cancellationToken = default)
        {
            this._logger.Information(value.ToString());
            return Task.CompletedTask;
        }

        public override void WriteLine()
        {
            this._logger.Information(string.Empty);
        }

        public override void WriteLine(bool value)
        {
            this._logger.Information(value.ToString());
        }

        public override void WriteLine(char value)
        {
            this._logger.Information(value.ToString());
        }

        public override void WriteLine(char[] buffer)
        {
            this._logger.Information(new string(buffer));
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            this._logger.Information(new string(buffer));
        }

        public override void WriteLine(decimal value)
        {
            this._logger.Information(value.ToString());
        }

        public override void WriteLine(double value)
        {
            this._logger.Information(value.ToString());
        }

        public override void WriteLine(int value)
        {
            this._logger.Information(value.ToString());
        }

        public override void WriteLine(long value)
        {
            this._logger.Information(value.ToString());
        }

        public override void WriteLine(object value)
        {
            this._logger.Information(value.ToString());
        }

        public override void WriteLine(ReadOnlySpan<char> buffer)
        {
            this._logger.Information(new string(buffer));
        }

        public override void WriteLine(float value)
        {
            this._logger.Information(value.ToString());
        }

        public override void WriteLine(string value)
        {
            this._logger.Information(value);
        }

        public override void WriteLine(string format, object arg0)
        {
            this._logger.Information(string.Format(format, arg0));
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            this._logger.Information(string.Format(format, arg0, arg1));
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            this._logger.Information(string.Format(format, arg0, arg1, arg2));
        }

        public override void WriteLine(string format, params object[] arg)
        {
            this._logger.Information(string.Format(format, arg));
        }

        public override void WriteLine(StringBuilder value)
        {
            this._logger.Information(value.ToString());
        }

        public override void WriteLine(uint value)
        {
            this._logger.Information(value.ToString());
        }

        public override void WriteLine(ulong value)
        {
            this._logger.Information(value.ToString());
        }

        public override Task WriteLineAsync()
        {
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(char value)
        {
            this._logger.Information(value.ToString());
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            this._logger.Information(new string(buffer));
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("not implemented");
        }

        public override Task WriteLineAsync(string value)
        {
            this._logger.Information(value.ToString());
            return Task.CompletedTask;
        }

        public override Task WriteLineAsync(StringBuilder value, CancellationToken cancellationToken = default)
        {
            this._logger.Information(value.ToString());
            return Task.CompletedTask;
        }
    }
}
