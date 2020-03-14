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


        public async Task Insert(Decision decision, Trend3Seconds trend3Seconds, Trend15Seconds trend15Seconds, Trend2Minutes trend2Minutes,
            Trend10Minutes trend10Minutes, Trend2Hours trend2Hours, CancellationToken cancellationToken)

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
                        com.Parameters.Add("p_decision", NpgsqlTypes.NpgsqlDbType.Text).Value = decision.Action;
                        com.Parameters.Add("p_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = decision.Price;
                        com.Parameters.Add("p_seconds5", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend3Seconds.Seconds5;
                        com.Parameters.Add("p_seconds10", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend3Seconds.Seconds10;
                        com.Parameters.Add("p_seconds15", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend3Seconds.Seconds15;
                        com.Parameters.Add("p_seconds30", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend3Seconds.Seconds30;
                        com.Parameters.Add("p_seconds45", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend15Seconds.Seconds45;
                        com.Parameters.Add("p_minute1", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend15Seconds.Minutes1;
                        com.Parameters.Add("p_minutes2", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend15Seconds.Minutes2;
                        com.Parameters.Add("p_minutes3", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend15Seconds.Minutes3;
                        com.Parameters.Add("p_minutes5", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Minutes.Minutes5;
                        com.Parameters.Add("p_minutes7", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Minutes.Minutes7;
                        com.Parameters.Add("p_minutes10", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Minutes.Minutes10;
                        com.Parameters.Add("p_minutes15", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Minutes.Minutes15;
                        com.Parameters.Add("p_minutes30", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend10Minutes.Minutes30;
                        com.Parameters.Add("p_hour1", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend10Minutes.Hours1;
                        com.Parameters.Add("p_hours2", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend10Minutes.Hours2;
                        com.Parameters.Add("p_hours3", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend10Minutes.Hours3;
                        com.Parameters.Add("p_hours6", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Hours.Hours6;
                        com.Parameters.Add("p_hours12", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Hours.Hours12;
                        com.Parameters.Add("p_hours18", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Hours.Hours18;
                        com.Parameters.Add("p_day1", NpgsqlTypes.NpgsqlDbType.Numeric).Value = trend2Hours.Day1;

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

                        foreach(var pp in pushedProperties)
                        {
                            pp.Dispose();
                        }
                    }
                }
            }
        }

        public async Task<IEnumerable<Trend3Seconds>> Get3SecondsTrend(CancellationToken cancellationToken)
        {
            var trends = new List<Trend3Seconds>();
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
                                    )
                                {
                                    continue;
                                }

                                pushedProperty = LogContext.PushProperty("reader", reader);
                                
                                trends.Add(new Trend3Seconds(
                                    reader.GetString(0),
                                    reader.GetInt32(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5)));
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

        public async Task<IEnumerable<Trend15Seconds>> Get15SecondsTrend(CancellationToken cancellationToken)
        {
            var trends = new List<Trend15Seconds>();
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
                                    )
                                {
                                    continue;
                                }

                                pushedProperty = LogContext.PushProperty("reader", reader);
                                
                                trends.Add(new Trend15Seconds(
                                    reader.GetString(0),
                                    reader.GetInt32(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5)));
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

        public async Task<IEnumerable<Trend2Minutes>> Get2MinutesTrend(CancellationToken cancellationToken)
        {
            var trends = new List<Trend2Minutes>();
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
                                    )
                                {
                                    continue;
                                }

                                pushedProperty = LogContext.PushProperty("reader", reader);
                                
                                trends.Add(new Trend2Minutes(
                                    reader.GetString(0),
                                    reader.GetInt32(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5)));
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

        public async Task<IEnumerable<Trend10Minutes>> Get10MinutesTrend(CancellationToken cancellationToken)
        {
            var trends = new List<Trend10Minutes>();
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
                                    )
                                {
                                    continue;
                                }

                                pushedProperty = LogContext.PushProperty("reader", reader);
                                
                                trends.Add(new Trend10Minutes(
                                    reader.GetString(0),
                                    reader.GetInt32(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5));
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

        public async Task<IEnumerable<Trend2Hours>> Get2HoursTrend(CancellationToken cancellationToken)
        {
            var trends = new List<Trend2Hours>();
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
                                    )
                                {
                                    continue;
                                }

                                pushedProperty = LogContext.PushProperty("reader", reader);
                                
                                trends.Add(new Trend2Hours(
                                    reader.GetString(0),
                                    reader.GetInt32(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5)));
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
                                
                                pushedProperty = LogContext.PushProperty("reader", reader);

                                currentPrices.Add(new CurrentPrice(
                                    reader.GetString(0),
                                    reader.GetDateTime(1),
                                    reader.GetDecimal(1),
                                    reader.GetDecimal(2),
                                    reader.GetDecimal(3),
                                    reader.GetDecimal(4),
                                    reader.GetDecimal(5),
                                    reader.GetDecimal(6)));
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
