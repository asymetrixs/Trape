using binance.cli.DataLayer.Models;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using log4net;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace binance.cli.DataLayer
{
    public class CoinTradeContext : DbContext, ICoinTradeContext
    {
        #region Fields

        /// <summary>
        /// Logger
        /// </summary>
        private ILog _logger;

        /// <summary>
        /// Connection String
        /// </summary>
        private string _connectionString;

        #endregion Fields

        public CoinTradeContext(string connectionString)
            : base()
        {
            this._logger = LogManager.GetLogger(typeof(CoinTradeContext));
            this._connectionString = connectionString;
        }

        public DbSet<CandleStick> CandleSticks { get; private set; }

        public DbSet<SymbolPrice> SymbolPrices { get; private set; }

        public async Task InsertCandleStick(GetKlinesCandlesticksRequest request, KlineCandleStickResponse result, CancellationToken cancellationToken)
        {
            if(null == result || null == request)
            {
                return;
            }

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("insert_candlestick", con))
                {
                    try
                    {
                        com.CommandType = CommandType.StoredProcedure;

                        com.Parameters.Add("p_open_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = result.OpenTime;
                        com.Parameters.Add("p_close_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = result.CloseTime;
                        com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = request.Symbol;
                        com.Parameters.Add("p_interval", NpgsqlTypes.NpgsqlDbType.Text).Value = request.Interval.ToString();
                        com.Parameters.Add("p_open", NpgsqlTypes.NpgsqlDbType.Numeric).Value = result.Open;
                        com.Parameters.Add("p_close", NpgsqlTypes.NpgsqlDbType.Numeric).Value = result.Close;
                        com.Parameters.Add("p_high", NpgsqlTypes.NpgsqlDbType.Numeric).Value = result.High;
                        com.Parameters.Add("p_low", NpgsqlTypes.NpgsqlDbType.Numeric).Value = result.Low;
                        com.Parameters.Add("p_number_of_trades", NpgsqlTypes.NpgsqlDbType.Integer).Value = result.NumberOfTrades;
                        com.Parameters.Add("p_quote_assed_volume", NpgsqlTypes.NpgsqlDbType.Numeric).Value = result.QuoteAssetVolume;
                        com.Parameters.Add("p_taker_buy_base_assed_volume", NpgsqlTypes.NpgsqlDbType.Numeric).Value = result.TakerBuyBaseAssetVolume;
                        com.Parameters.Add("p_taker_buy_quote_assed_volume", NpgsqlTypes.NpgsqlDbType.Numeric).Value = result.TakerBuyQuoteAssetVolume;
                        com.Parameters.Add("p_quote_asset_volume", NpgsqlTypes.NpgsqlDbType.Numeric).Value = result.QuoteAssetVolume;
                        com.Parameters.Add("p_volume", NpgsqlTypes.NpgsqlDbType.Numeric).Value = result.Volume;

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

        public async Task InsertPrice(DateTimeOffset datetime, SymbolPriceResponse price, CancellationToken cancellationToken)
        {
            if(null == price)
            {
                return;
            }

            using (var con = new NpgsqlConnection(this._connectionString))
            {
                using (var com = new NpgsqlCommand("insert_price", con))
                {
                    try
                    {
                        com.CommandType = CommandType.StoredProcedure;

                        datetime = new DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second);

                        com.Parameters.Add("p_datetime", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = datetime;
                        com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = price.Symbol;                        
                        com.Parameters.Add("p_price", NpgsqlTypes.NpgsqlDbType.Numeric).Value = price.Price;                        
                        
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
