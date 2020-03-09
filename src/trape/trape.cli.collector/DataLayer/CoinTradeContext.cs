using Binance.Net.Objects;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace trape.cli.collector.DataLayer
{
    public class CoinTradeContext : DbContext, ICoinTradeContext
    {
        #region Fields

        private ILogger _logger;

        private string _connectionString;

        #endregion Fields

        public CoinTradeContext(ILogger logger)
            : base()
        {
            if(null == logger)
            {
                throw new ArgumentNullException("Paramter cannot be NULL");
            }

            this._logger = logger;
            this._connectionString = Configuration.GetConnectionString("CoinTradeDB");
        }


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
                    }
                }
            }
        }

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
                    }
                }
            }
        }
        
    }
}
