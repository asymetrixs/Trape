using Binance.Net.Objects;
using Binance.Net.Objects.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache.Models;

namespace trape.cli.trader.DataLayer
{
    public class TrapeContext : DbContext, ITrapeContext
    {
        #region Fields

        private readonly ILogger _logger;

        private readonly string _connectionString;

        #endregion Fields

        public TrapeContext(ILogger logger)
            : base()
        {
            if (null == logger)
            {
                throw new ArgumentNullException("Paramter cannot be NULL");
            }

            this._logger = logger;
            this._connectionString = Configuration.GetConnectionString("CoinTradeDB");
        }


        public async Task Insert(Analyze.Recommendation recommendation, Stats3s stat3s, Stats15s stat15s, Stats2m stat2m,
            Stats10m stat10m, Stats2h stat2h, CancellationToken cancellationToken)

        {
            if (null == stat3s || null == stat15s || null == stat2m || null == stat10m || null == stat2h || null == recommendation)
            {
                return;
            }

            var pushedProperties = new List<IDisposable>();

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("insert_recommendation", con))
                {
                    try
                    {
                        this._logger.Verbose($"Executing {com.CommandText}");

                        pushedProperties.Add(LogContext.PushProperty("recommendation", recommendation));
                        pushedProperties.Add(LogContext.PushProperty("stat3s", stat3s));
                        pushedProperties.Add(LogContext.PushProperty("stat15s", stat15s));
                        pushedProperties.Add(LogContext.PushProperty("stat2m", stat2m));
                        pushedProperties.Add(LogContext.PushProperty("stat10m", stat10m));
                        pushedProperties.Add(LogContext.PushProperty("stat2h", stat2h));

                        com.CommandType = CommandType.StoredProcedure;

                        com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = recommendation.Symbol;
                        com.Parameters.Add("p_decision", NpgsqlTypes.NpgsqlDbType.Text).Value = recommendation.Action.ToString();
                        com.Parameters.Add("p_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = recommendation.Price;
                        com.Parameters.Add("p_slope5s", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat3s.Slope5s;
                        com.Parameters.Add("p_movav5s", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat3s.MovingAverage5s;
                        com.Parameters.Add("p_slope10s", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat3s.Slope10s;
                        com.Parameters.Add("p_movav10s", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat3s.MovingAverage10s;
                        com.Parameters.Add("p_slope15s", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat3s.Slope15s;
                        com.Parameters.Add("p_movav15s", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat3s.MovingAverage15s;
                        com.Parameters.Add("p_slope30s", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat3s.Slope30s;
                        com.Parameters.Add("p_movav30s", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat3s.MovingAverage30s;
                        com.Parameters.Add("p_slope45s", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat15s.Slope45s;
                        com.Parameters.Add("p_movav45s", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat15s.MovingAverage45s;
                        com.Parameters.Add("p_slope1m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat15s.Slope1m;
                        com.Parameters.Add("p_movav1m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat15s.MovingAverage1m;
                        com.Parameters.Add("p_slope2m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat15s.Slope2m;
                        com.Parameters.Add("p_movav2m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat15s.MovingAverage2m;
                        com.Parameters.Add("p_slope3m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat15s.Slope3m;
                        com.Parameters.Add("p_movav3m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat15s.MovingAverage3m;
                        com.Parameters.Add("p_slope5m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2m.Slope5m;
                        com.Parameters.Add("p_movav5m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2m.MovingAverage5m;
                        com.Parameters.Add("p_slope7m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2m.Slope7m;
                        com.Parameters.Add("p_movav7m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2m.MovingAverage7m;
                        com.Parameters.Add("p_slope10m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2m.Slope10m;
                        com.Parameters.Add("p_movav10m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2m.MovingAverage10m;
                        com.Parameters.Add("p_slope15m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2m.Slope15m;
                        com.Parameters.Add("p_movav15m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2m.MovingAverage15m;
                        com.Parameters.Add("p_slope30m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat10m.Slope30m;
                        com.Parameters.Add("p_movav30m", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat10m.MovingAverage30m;
                        com.Parameters.Add("p_slope1h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat10m.Slope1h;
                        com.Parameters.Add("p_movav1h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat10m.MovingAverage1h;
                        com.Parameters.Add("p_slope2h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat10m.Slope2h;
                        com.Parameters.Add("p_movav2h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat10m.MovingAverage2h;
                        com.Parameters.Add("p_slope3h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat10m.Slope3h;
                        com.Parameters.Add("p_movav3h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat10m.MovingAverage3h;
                        com.Parameters.Add("p_slope6h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2h.Slope6h;
                        com.Parameters.Add("p_movav6h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2h.MovingAverage6h;
                        com.Parameters.Add("p_slope12h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2h.Slope12h;
                        com.Parameters.Add("p_movav12h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2h.MovingAverage12h;
                        com.Parameters.Add("p_slope18h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2h.Slope18h;
                        com.Parameters.Add("p_movav18h", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2h.MovingAverage18h;
                        com.Parameters.Add("p_slope1d", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2h.Slope1d;
                        com.Parameters.Add("p_movav1d", NpgsqlTypes.NpgsqlDbType.Numeric).Value = stat2h.MovingAverage1d;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        await com.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this._logger.Fatal(ex.Message, ex);
                        }
#if DEBUG
                        throw;
#endif
                    }
                    finally
                    {
                        con.Close();

                        foreach (var pp in pushedProperties)
                        {
                            pp.Dispose();
                        }
                    }
                }
            }
        }

        public async Task<IEnumerable<Stats3s>> Get3SecondsTrend(CancellationToken cancellationToken)
        {
            var trends = new List<Stats3s>();
            IDisposable pushedProperty = null;

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("stats_3s", con))
                {
                    try
                    {
                        this._logger.Verbose($"Executing {com.CommandText}");

                        com.CommandType = CommandType.StoredProcedure;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
                        {
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                // Skip if values are NULL
                                if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false)
                                    )
                                {
                                    continue;
                                }

                                pushedProperty = LogContext.PushProperty("reader", reader);

                                trends.Add(new Stats3s(
                                    reader.GetString(0),
                                    reader.GetInt32(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5),
                                    reader.GetDecimal(6),
                                    reader.GetDecimal(7),
                                    reader.GetDecimal(8),
                                    reader.GetDecimal(9)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this._logger.Fatal(ex.Message, ex);
                        }
#if DEBUG
                        throw;
#endif
                    }
                    finally
                    {
                        pushedProperty?.Dispose();

                        con.Close();
                    }
                }
            }

            return trends;
        }

        public async Task<IEnumerable<Stats15s>> Get15SecondsTrend(CancellationToken cancellationToken)
        {
            var trends = new List<Stats15s>();
            IDisposable pushedProperty = null;

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("stats_15s", con))
                {
                    try
                    {
                        this._logger.Verbose($"Executing {com.CommandText}");

                        com.CommandType = CommandType.StoredProcedure;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
                        {
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                // Skip if values are NULL
                                if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false)
                                    )
                                {
                                    continue;
                                }

                                pushedProperty = LogContext.PushProperty("reader", reader);

                                trends.Add(new Stats15s(
                                    reader.GetString(0),
                                    reader.GetInt32(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5),
                                    reader.GetDecimal(6),
                                    reader.GetDecimal(7),
                                    reader.GetDecimal(8),
                                    reader.GetDecimal(9)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this._logger.Fatal(ex.Message, ex);
                        }
#if DEBUG
                        throw;
#endif
                    }
                    finally
                    {
                        pushedProperty?.Dispose();

                        con.Close();
                    }
                }
            }

            return trends;
        }

        public async Task<IEnumerable<Stats2m>> Get2MinutesTrend(CancellationToken cancellationToken)
        {
            var trends = new List<Stats2m>();
            IDisposable pushedProperty = null;

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("stats_2m", con))
                {
                    try
                    {
                        this._logger.Verbose($"Executing {com.CommandText}");

                        com.CommandType = CommandType.StoredProcedure;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
                        {
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                // Skip if values are NULL
                                if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false)
                                    )
                                {
                                    continue;
                                }

                                pushedProperty = LogContext.PushProperty("reader", reader);

                                trends.Add(new Stats2m(
                                    reader.GetString(0),
                                    reader.GetInt32(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5),
                                    reader.GetDecimal(6),
                                    reader.GetDecimal(7),
                                    reader.GetDecimal(8),
                                    reader.GetDecimal(9)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this._logger.Fatal(ex.Message, ex);
                        }
#if DEBUG
                        throw;
#endif
                    }
                    finally
                    {
                        pushedProperty?.Dispose();

                        con.Close();
                    }
                }
            }

            return trends;
        }

        public async Task<IEnumerable<Stats10m>> Get10MinutesTrend(CancellationToken cancellationToken)
        {
            var trends = new List<Stats10m>();
            IDisposable pushedProperty = null;

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("stats_10m", con))
                {
                    try
                    {
                        this._logger.Verbose($"Executing {com.CommandText}");

                        com.CommandType = CommandType.StoredProcedure;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
                        {
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                // Skip if values are NULL
                                if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false)
                                    )
                                {
                                    continue;
                                }

                                pushedProperty = LogContext.PushProperty("reader", reader);

                                trends.Add(new Stats10m(
                                    reader.GetString(0),
                                    reader.GetInt32(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5),
                                    reader.GetDecimal(6),
                                    reader.GetDecimal(7),
                                    reader.GetDecimal(8),
                                    reader.GetDecimal(9)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this._logger.Fatal(ex.Message, ex);
                        }
#if DEBUG
                        throw;
#endif
                    }
                    finally
                    {
                        pushedProperty?.Dispose();

                        con.Close();
                    }
                }
            }

            return trends;
        }

        public async Task<IEnumerable<Stats2h>> Get2HoursTrend(CancellationToken cancellationToken)
        {
            var trends = new List<Stats2h>();
            IDisposable pushedProperty = null;

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("stats_2h", con))
                {
                    try
                    {
                        this._logger.Verbose($"Executing {com.CommandText}");

                        com.CommandType = CommandType.StoredProcedure;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
                        {
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                // Skip if values are NULL
                                if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false)
                                    )
                                {
                                    continue;
                                }

                                pushedProperty = LogContext.PushProperty("reader", reader);

                                trends.Add(new Stats2h(
                                    reader.GetString(0),
                                    reader.GetInt32(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5),
                                    reader.GetDecimal(6),
                                    reader.GetDecimal(7),
                                    reader.GetDecimal(8),
                                    reader.GetDecimal(9)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this._logger.Fatal(ex.Message, ex);
                        }
#if DEBUG
                        throw;
#endif
                    }
                    finally
                    {
                        pushedProperty?.Dispose();

                        con.Close();
                    }
                }
            }

            return trends;
        }

        public async Task<decimal> GetCurrentPrice(string symbol, CancellationToken cancellationToken)
        {
            var price = default(decimal);
            IDisposable pushedProperty = null;

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("current_price", con))
                {
                    try
                    {
                        this._logger.Verbose($"Executing {com.CommandText}");

                        com.CommandType = CommandType.StoredProcedure;

                        com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = symbol;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        var obj = await com.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

                        pushedProperty = LogContext.PushProperty("obj", obj);

                        price = (decimal)obj;
                    }
                    catch (InvalidCastException)
                    {
                        this._logger.Error("Value is not decimal");
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this._logger.Fatal(ex.Message, ex);
                        }
#if DEBUG
                        throw;
#endif
                    }
                    finally
                    {
                        pushedProperty?.Dispose();

                        con.Close();
                    }
                }
            }

            return price;
        }

        public async Task<IEnumerable<CurrentPrice>> GetCurrentPrice(CancellationToken cancellationToken)
        {
            var currentPrices = new List<CurrentPrice>();
            IDisposable pushedProperty = null;

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("current_price", con))
                {
                    try
                    {
                        this._logger.Verbose($"Executing {com.CommandText}");

                        com.CommandType = CommandType.StoredProcedure;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
                        {
                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                pushedProperty = LogContext.PushProperty("reader", reader);

                                // Skip if values are NULL
                                if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false)
                                    || await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
                                    )
                                {
                                    continue;
                                }

                                currentPrices.Add(new CurrentPrice(
                                    reader.GetString(0),
                                    reader.GetDateTime(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5),
                                    reader.GetDecimal(6),
                                    reader.GetDecimal(7)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this._logger.Fatal(ex.Message, ex);
                        }
#if DEBUG
                        throw;
#endif
                    }
                    finally
                    {
                        pushedProperty?.Dispose();

                        con.Close();
                    }
                }
            }

            return currentPrices;
        }

        public async Task Insert(IEnumerable<BinanceStreamBalance> binanceStreamBalances, CancellationToken cancellationToken)
        {
            if (null == binanceStreamBalances || !binanceStreamBalances.Any())
            {
                return;
            }

            var pushedProperties = new List<IDisposable>();

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("insert_binance_stream_balance", con))
                {
                    try
                    {
                        this._logger.Verbose($"Executing {com.CommandText}");

                        pushedProperties.Add(LogContext.PushProperty("binanceStreamBalances", binanceStreamBalances));

                        com.CommandType = CommandType.StoredProcedure;

                        // p_symbol TEXT, p_free NUMERIC, p_locked NUMERIC, p_total NUMERIC
                        var pAsset = com.Parameters.Add("p_asset", NpgsqlTypes.NpgsqlDbType.Text);
                        var pFree = com.Parameters.Add("p_free", NpgsqlTypes.NpgsqlDbType.Numeric);
                        var pLocked = com.Parameters.Add("p_locked", NpgsqlTypes.NpgsqlDbType.Numeric);
                        var pTotal = com.Parameters.Add("p_total", NpgsqlTypes.NpgsqlDbType.Numeric);

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        await com.PrepareAsync().ConfigureAwait(false);

                        foreach (var bsb in binanceStreamBalances)
                        {
                            pAsset.Value = bsb.Asset;
                            pFree.Value = bsb.Free;
                            pLocked.Value = bsb.Locked;
                            pTotal.Value = bsb.Total;

                            await com.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        }

                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this._logger.Fatal(ex.Message, ex);
                        }
#if DEBUG
                        throw;
#endif
                    }
                    finally
                    {
                        con.Close();

                        foreach (var pp in pushedProperties)
                        {
                            pp.Dispose();
                        }
                    }
                }
            }
        }

        public async Task Insert(BinanceStreamBalanceUpdate binanceStreamBalanceUpdate, CancellationToken cancellationToken)

        {
            if (null == binanceStreamBalanceUpdate)
            {
                return;
            }

            var pushedProperties = new List<IDisposable>();

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("insert_binance_stream_balance_update", con))
                {
                    try
                    {
                        this._logger.Verbose($"Executing {com.CommandText}");

                        pushedProperties.Add(LogContext.PushProperty("binanceStreamBalanceUpdate", binanceStreamBalanceUpdate));

                        com.CommandType = CommandType.StoredProcedure;

                        com.Parameters.Add("p_event_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = binanceStreamBalanceUpdate.EventTime;
                        com.Parameters.Add("p_event", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamBalanceUpdate.Event;
                        com.Parameters.Add("p_asset", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamBalanceUpdate.Asset;
                        com.Parameters.Add("p_balance_delta", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamBalanceUpdate.BalanceDelta;
                        com.Parameters.Add("p_clear_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = binanceStreamBalanceUpdate.ClearTime;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        await com.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this._logger.Fatal(ex.Message, ex);
                        }
#if DEBUG
                        throw;
#endif
                    }
                    finally
                    {
                        con.Close();

                        foreach (var pp in pushedProperties)
                        {
                            pp.Dispose();
                        }
                    }
                }
            }
        }


        public async Task Insert(BinanceStreamOrderList binanceStreamOrderList, CancellationToken cancellationToken)

        {
            if (null == binanceStreamOrderList)
            {
                return;
            }

            var pushedProperties = new List<IDisposable>();

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                using (var transaction = await con.BeginTransactionAsync(cancellationToken))
                {
                    using (var com = new NpgsqlCommand("insert_binance_stream_order_list", con))
                    {
                        try
                        {
                            this._logger.Verbose($"Executing {com.CommandText}");

                            pushedProperties.Add(LogContext.PushProperty("binanceStreamOrderList", binanceStreamOrderList));

                            com.CommandType = CommandType.StoredProcedure;

                            com.Parameters.Add("p_event_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = binanceStreamOrderList.EventTime;
                            com.Parameters.Add("p_event", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamOrderList.Event;
                            com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamOrderList.Symbol;
                            com.Parameters.Add("p_transaction_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = binanceStreamOrderList.TransactionTime;
                            com.Parameters.Add("p_order_list_id", NpgsqlTypes.NpgsqlDbType.Bigint).Value = binanceStreamOrderList.OrderListId;
                            com.Parameters.Add("p_contingency_type", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamOrderList.ContingencyType;
                            com.Parameters.Add("p_list_status_type", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamOrderList.ListStatusType;
                            com.Parameters.Add("p_list_order_status", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamOrderList.ListOrderStatus;
                            com.Parameters.Add("p_list_client_order_id", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamOrderList.ListClientOrderId;

                            await com.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                            using (var com2 = new NpgsqlCommand("insert_binance_stream_order_list", con))
                            {
                                this._logger.Verbose($"Executing {com2.CommandText}");

                                pushedProperties.Add(LogContext.PushProperty("binanceStreamOrderList-orders", binanceStreamOrderList.Orders));

                                com.CommandType = CommandType.StoredProcedure;

                                // p_symbol TEXT, p_free NUMERIC, p_locked NUMERIC, p_total NUMERIC
                                var pSymbol = com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text);
                                var pOrderId = com.Parameters.Add("p_order_id", NpgsqlTypes.NpgsqlDbType.Bigint);
                                var pClientOrderId = com.Parameters.Add("p_client_order_id", NpgsqlTypes.NpgsqlDbType.Text);

                                await com2.PrepareAsync().ConfigureAwait(false);

                                foreach (var order in binanceStreamOrderList.Orders)
                                {
                                    pSymbol.Value = order.Symbol;
                                    pOrderId.Value = order.OrderId;
                                    pClientOrderId.Value = order.ClientOrderId;

                                    await com2.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }

                            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                this._logger.Fatal(ex.Message, ex);
                            }
#if DEBUG
                            throw;
#endif
                        }
                        finally
                        {
                            con.Close();

                            foreach (var pp in pushedProperties)
                            {
                                pp.Dispose();
                            }
                        }
                    }
                }
            }
        }
    }
}
