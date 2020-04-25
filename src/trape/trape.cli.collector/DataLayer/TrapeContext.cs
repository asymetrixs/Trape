using Binance.Net.Objects;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using trape.cli.collector.DataLayer.Models;

namespace trape.cli.collector.DataLayer
{
    /// <summary>
    /// Class implementing the TrapeContext
    /// </summary>
    public class TrapeContext : DbContext, ITrapeContext
    {
        #region Fields

        /// <summary>
        /// Logger
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// Connection String
        /// </summary>
        private string _connectionString;

        /// <summary>
        /// Disposed
        /// </summary>
        private bool _disposed;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Constructs a new instance of the <c>TrapeContext</c> class
        /// </summary>
        /// <param name="logger"></param>
        public TrapeContext(ILogger logger)
            : base()
        {
            #region Argument checks

            if (null == logger)
            {
                throw new ArgumentNullException(paramName: nameof(logger));
            }

            #endregion

            this._logger = logger.ForContext<TrapeContext>();
            this._connectionString = Config.GetConnectionString("CoinTradeDB");
            this._disposed = false;
        }

        /// <summary>
        /// Configure
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseNpgsql(this._connectionString);

            base.OnConfiguring(optionsBuilder);
        }

        #endregion

        #region Properties

        public DbSet<Symbol> Symbols { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Inserts <c>BinanceStreamTick</c> instances into the database
        /// </summary>
        /// <param name="binanceStreamTick"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Insert(BinanceStreamTick binanceStreamTick, CancellationToken cancellationToken)
        {
            if (null == binanceStreamTick)
            {
                return;
            }

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("insert_binance_stream_tick", con))
                {
                    try
                    {
                        this._logger.Verbose("Executing insert_binance_stream_tick ");

                        com.CommandType = CommandType.StoredProcedure;

                        com.Parameters.Add("p_event", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamTick.Event;
                        com.Parameters.Add("p_event_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = binanceStreamTick.EventTime;
                        com.Parameters.Add("p_total_trades", NpgsqlTypes.NpgsqlDbType.Bigint).Value = binanceStreamTick.TotalTrades;
                        com.Parameters.Add("p_last_trade_id", NpgsqlTypes.NpgsqlDbType.Bigint).Value = binanceStreamTick.LastTradeId;
                        com.Parameters.Add("p_first_trade_id", NpgsqlTypes.NpgsqlDbType.Bigint).Value = binanceStreamTick.FirstTradeId;
                        com.Parameters.Add("p_total_traded_quote_asset_volume", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.TotalTradedQuoteAssetVolume;
                        com.Parameters.Add("p_total_traded_base_asset_volume", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.TotalTradedBaseAssetVolume;
                        com.Parameters.Add("p_low_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.LowPrice;
                        com.Parameters.Add("p_high_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.HighPrice;
                        com.Parameters.Add("p_open_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.OpenPrice;
                        com.Parameters.Add("p_best_ask_quantity", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.BestAskQuantity;
                        com.Parameters.Add("p_best_ask_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.BestAskPrice;
                        com.Parameters.Add("p_best_bid_quantity", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.BestBidQuantity;
                        com.Parameters.Add("p_best_bid_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.BestBidPrice;
                        com.Parameters.Add("p_close_trades_quantity", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.CloseTradesQuantity;
                        com.Parameters.Add("p_current_day_close_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.CurrentDayClosePrice;
                        com.Parameters.Add("p_prev_day_close_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.PrevDayClosePrice;
                        com.Parameters.Add("p_weighted_average", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.WeightedAverage;
                        com.Parameters.Add("p_price_change_percentage", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.PriceChangePercentage;
                        com.Parameters.Add("p_price_change", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamTick.PriceChange;
                        com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamTick.Symbol;
                        com.Parameters.Add("p_statistics_open_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = binanceStreamTick.StatisticsOpenTime;
                        com.Parameters.Add("p_statistics_close_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = binanceStreamTick.StatisticsCloseTime;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        await com.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException oce)
                    {
                        this._logger.Debug(oce, oce.Message);
                    }
                    catch (NpgsqlException ex)
                    {
                        this._logger.Fatal(ex, ex.Message, com.CommandText);
                    }
                    catch (Exception e)
                    {
                        this._logger.Fatal($"General Exception: {e.Message}");
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Inserts <c>BinanceStreamKlineData</c> instances into the database
        /// </summary>
        /// <param name="binanceStreamKlineData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Insert(BinanceStreamKlineData binanceStreamKlineData, CancellationToken cancellationToken)
        {
            if (null == binanceStreamKlineData)
            {
                return;
            }

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("insert_binance_stream_kline_data", con))
                {
                    try
                    {
                        this._logger.Verbose("Executing insert_binance_stream_kline_data ");

                        com.CommandType = CommandType.StoredProcedure;

                        com.Parameters.Add("p_event", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamKlineData.Event;
                        com.Parameters.Add("p_event_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = binanceStreamKlineData.EventTime;
                        com.Parameters.Add("p_close", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamKlineData.Data.Close;
                        com.Parameters.Add("p_close_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = binanceStreamKlineData.Data.CloseTime;
                        com.Parameters.Add("p_final", NpgsqlTypes.NpgsqlDbType.Boolean).Value = binanceStreamKlineData.Data.Final;
                        com.Parameters.Add("p_first_trade_id", NpgsqlTypes.NpgsqlDbType.Bigint).Value = binanceStreamKlineData.Data.FirstTrade;
                        com.Parameters.Add("p_high_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamKlineData.Data.High;
                        com.Parameters.Add("p_interval", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamKlineData.Data.Interval.ToString();
                        com.Parameters.Add("p_last_trade_id", NpgsqlTypes.NpgsqlDbType.Bigint).Value = binanceStreamKlineData.Data.LastTrade;
                        com.Parameters.Add("p_low_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamKlineData.Data.Low;
                        com.Parameters.Add("p_open_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamKlineData.Data.Open;
                        com.Parameters.Add("p_open_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = binanceStreamKlineData.Data.OpenTime;
                        com.Parameters.Add("p_quote_asset_volume", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamKlineData.Data.QuoteAssetVolume;
                        com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceStreamKlineData.Data.Symbol;
                        com.Parameters.Add("p_taker_buy_base_asset_volume", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamKlineData.Data.TakerBuyBaseAssetVolume;
                        com.Parameters.Add("p_taker_buy_quote_asset_volume", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamKlineData.Data.TakerBuyQuoteAssetVolume;
                        com.Parameters.Add("p_trade_count", NpgsqlTypes.NpgsqlDbType.Integer).Value = binanceStreamKlineData.Data.TradeCount;
                        com.Parameters.Add("p_volume", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceStreamKlineData.Data.Volume;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        await com.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException oce)
                    {
                        this._logger.Debug(oce, oce.Message);
                    }
                    catch (NpgsqlException ex)
                    {
                        this._logger.Fatal(ex, ex.Message, com.CommandText);
                    }
                    catch (Exception e)
                    {
                        this._logger.Fatal($"General Exception: {e.Message}");
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Inserts <c>BinanceBookTick</c> instances into the database
        /// </summary>
        /// <param name="binanceBookTick"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Insert(BinanceBookTick binanceBookTick, CancellationToken cancellationToken)
        {
            if (null == binanceBookTick)
            {
                return;
            }

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("insert_binance_book_tick", con))
                {
                    try
                    {
                        this._logger.Verbose("Executing insert_binance_book_tick");

                        com.CommandType = CommandType.StoredProcedure;

                        com.Parameters.Add("p_event_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = DateTime.UtcNow;
                        com.Parameters.Add("p_best_ask_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceBookTick.BestAskPrice;
                        com.Parameters.Add("p_best_ask_quantity", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceBookTick.BestAskQuantity;
                        com.Parameters.Add("p_best_bid_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceBookTick.BestBidPrice;
                        com.Parameters.Add("p_best_bid_quantity", NpgsqlTypes.NpgsqlDbType.Numeric).Value = binanceBookTick.BestBidQuantity;
                        com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = binanceBookTick.Symbol;
                        com.Parameters.Add("p_update_id", NpgsqlTypes.NpgsqlDbType.Bigint).Value = binanceBookTick.UpdateId;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        await com.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException oce)
                    {
                        this._logger.Debug(oce, oce.Message);
                    }
                    catch (NpgsqlException ex)
                    {
                        this._logger.Fatal(ex, ex.Message, com.CommandText);
                    }
                    catch (Exception e)
                    {
                        this._logger.Fatal($"General Exception: {e.Message}");
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up the <c>BinanceBookTick</c>s
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<int> CleanUpBookTicks(CancellationToken cancellationToken)
        {
            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("cleanup_book_ticks", con))
                {
                    try
                    {
                        this._logger.Verbose("Executing cleanup_book_ticks ");

                        com.CommandType = CommandType.StoredProcedure;

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        var obj = await com.ExecuteScalarAsync().ConfigureAwait(false);

                        return (int)obj;
                    }
                    catch (OperationCanceledException oce)
                    {
                        this._logger.Debug(oce, oce.Message);
                    }
                    catch (NpgsqlException ex)
                    {
                        this._logger.Fatal(ex, ex.Message, com.CommandText);
                    }
                    catch (Exception e)
                    {
                        this._logger.Fatal($"General Exception: {e.Message}");
                    }
                    finally
                    {
                        con.Close();
                    }
                }

                return -1;
            }
        }

        #endregion

        #region Dispose
        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        public sealed override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual async void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this._logger = null;
                this._connectionString = null;

                await base.DisposeAsync();
            }

            this._disposed = true;
        }

        #endregion
    }
}
