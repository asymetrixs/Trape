using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.trader.Cache.Models;

namespace trape.cli.trader.DataLayer
{
    public class TrapeContext : DbContext, ITrapeContext
    {
        #region Fields

        private ILogger _logger;

        private string _connectionString;

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


        public async Task Insert(Decision.Decision decision, Stats3s trend3Seconds, Stats15s trend15Seconds, Stats2m trend2Minutes,
            Stats10m trend10Minutes, Stats2h trend2Hours, CancellationToken cancellationToken)

        {
            if (null == trend3Seconds || null == trend15Seconds || null == trend2Minutes || null == trend10Minutes || null == trend2Hours || null == decision)
            {
                return;
            }

            var pushedProperties = new List<IDisposable>();

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("insert_decision", con))
                {
                    try
                    {
                        this._logger.Debug("Executing insert_decision");

                        pushedProperties.Add(LogContext.PushProperty("decision", decision));
                        pushedProperties.Add(LogContext.PushProperty("decision", trend3Seconds));
                        pushedProperties.Add(LogContext.PushProperty("decision", trend15Seconds));
                        pushedProperties.Add(LogContext.PushProperty("decision", trend2Minutes));
                        pushedProperties.Add(LogContext.PushProperty("decision", trend10Minutes));
                        pushedProperties.Add(LogContext.PushProperty("decision", trend2Hours));

                        com.CommandType = CommandType.StoredProcedure;

                        com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = decision.Symbol;
                        com.Parameters.Add("p_decision", NpgsqlTypes.NpgsqlDbType.Text).Value = decision.Action.ToString() + "-" + decision.Indicator.ToString("0.0000");
                        com.Parameters.Add("p_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = decision.Price;
                        com.Parameters.Add("p_seconds5", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend3Seconds.Slope5s;
                        com.Parameters.Add("p_seconds10", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend3Seconds.Slope10s;
                        com.Parameters.Add("p_seconds15", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend3Seconds.Slope15s;
                        com.Parameters.Add("p_seconds30", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend3Seconds.Slope30s;
                        com.Parameters.Add("p_seconds45", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend15Seconds.Slope45s;
                        com.Parameters.Add("p_minute1", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend15Seconds.Slope1m;
                        com.Parameters.Add("p_minutes2", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend15Seconds.Slope2m;
                        com.Parameters.Add("p_minutes3", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend15Seconds.Slope3m;
                        com.Parameters.Add("p_minutes5", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Minutes.Slope5m;
                        com.Parameters.Add("p_minutes7", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Minutes.Slope7m;
                        com.Parameters.Add("p_minutes10", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Minutes.Slope10m;
                        com.Parameters.Add("p_minutes15", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Minutes.Slope15m;
                        com.Parameters.Add("p_minutes30", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend10Minutes.Slope30m;
                        com.Parameters.Add("p_hour1", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend10Minutes.Slope1h;
                        com.Parameters.Add("p_hours2", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend10Minutes.Slope2h;
                        com.Parameters.Add("p_hours3", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend10Minutes.Slope3h;
                        com.Parameters.Add("p_hours6", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Hours.Slope6h;
                        com.Parameters.Add("p_hours12", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Hours.Slope12h;
                        com.Parameters.Add("p_hours18", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Hours.Slope18h;
                        com.Parameters.Add("p_day1", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Hours.Slope1d;

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
                using (var com = new NpgsqlCommand("trends_3sec", con))
                {
                    try
                    {
                        this._logger.Verbose("Executing trends_3sec");

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
                using (var com = new NpgsqlCommand("trends_15sec", con))
                {
                    try
                    {
                        this._logger.Verbose("Executing trends_15sec");

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
                using (var com = new NpgsqlCommand("trends_2min", con))
                {
                    try
                    {
                        this._logger.Verbose("Executing trends_2min");

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
                using (var com = new NpgsqlCommand("trends_10min", con))
                {
                    try
                    {
                        this._logger.Verbose("Executing trends_10min");

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
                using (var com = new NpgsqlCommand("trends_2hours", con))
                {
                    try
                    {
                        this._logger.Verbose("Executing trends_2hours");

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
                        this._logger.Verbose("Executing current_price");

                        com.CommandType = CommandType.StoredProcedure;

                        com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = symbol;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        var obj = await com.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

                        pushedProperty = LogContext.PushProperty("obj", obj);

                        try
                        {
                            price = (decimal)obj;
                        }
                        catch
                        {
                            this._logger.Error("Value is not decimal: " + obj.ToString());
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
                        this._logger.Verbose("Executing current_price");

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
    }
}
