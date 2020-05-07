using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.NameTranslation;
using Serilog;
using Serilog.Context;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using trape.datalayer.Models;

namespace trape.datalayer
{
    public class TrapeContext : DbContext
    {
        #region Fields

        /// <summary>
        ///  Logger
        /// </summary>
        private ILogger _logger;

        #endregion

        #region Constructor

        public TrapeContext(DbContextOptions<TrapeContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <c>TrapeContext</c> class.
        /// </summary>
        /// <param name="options"></param>
        public TrapeContext(DbContextOptions<TrapeContext> options, ILogger logger)
        : base(options)
        {
            #region Argument checks

            _ = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            #endregion

            this._logger = logger.ForContext(typeof(TrapeContext));
        }

        #endregion

        public DbSet<AccountInfo> AccountInfos { get; set; }

        public DbSet<Balance> Balances { get; set; }

        public DbSet<BalanceUpdate> BalanceUpdates { get; set; }

        public DbSet<BookTick> BookTicks { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderList> OrderLists { get; set; }

        public DbSet<OrderUpdate> OrderUpdates { get; set; }

        public DbSet<Tick> Ticks { get; set; }

        public DbSet<Kline> Klines { get; set; }

        public DbSet<Symbol> Symbols { get; set; }

        public DbSet<Recommendation> Recommendations { get; set; }

        public DbSet<ClientOrder> ClientOrder { get; set; }

        public DbSet<PlacedOrder> PlacedOrders { get; set; }

        public DbSet<OrderTrade> OrderTrades { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => base.OnConfiguring(optionsBuilder);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder is null)
            {
                return;
            }

            modelBuilder.Entity<Stats3s>()
                .HasNoKey();

            modelBuilder.Entity<Stats15s>()
                .HasNoKey();

            modelBuilder.Entity<Stats2m>()
                .HasNoKey();

            modelBuilder.Entity<Stats10m>()
                .HasNoKey();

            modelBuilder.Entity<Stats2h>()
                .HasNoKey();

            modelBuilder.Entity<LatestMA10mAndMA30mCrossing>()
                .HasNoKey();

            modelBuilder.Entity<LatestMA30mAndMA1hCrossing>()
                .HasNoKey();

            modelBuilder.Entity<LatestMA1hAndMA3hCrossing>()
                .HasNoKey();

            // AccountInfo
            modelBuilder.Entity<AccountInfo>()
                .HasKey(a => a.Id);
            modelBuilder.Entity<AccountInfo>()
                .HasMany(a => a.Balances)
                .WithOne(b => b.AccountInfo)
                .HasForeignKey(b => b.AccountInfoId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<AccountInfo>()
                .HasIndex(a => a.Id);
            modelBuilder.Entity<Balance>()
                .HasIndex(a => a.AccountInfoId);

            // Balance
            modelBuilder.Entity<Balance>()
                .HasKey(b => b.Id);
            modelBuilder.Entity<Balance>()
                .HasIndex(b => b.Id);

            // Balance
            modelBuilder.Entity<BalanceUpdate>()
                .HasKey(b => b.Id);
            modelBuilder.Entity<BalanceUpdate>()
                .HasIndex(b => b.Id);

            // OrderUpdate
            modelBuilder.Entity<OrderUpdate>()
                .HasKey(o => o.Id);
            modelBuilder.Entity<OrderUpdate>()
                .HasIndex(o => o.Id);
            modelBuilder.Entity<OrderUpdate>()
                .HasIndex(o => o.OrderId);
            modelBuilder.Entity<OrderUpdate>()
                .HasIndex(o => o.ClientOrderId);
            modelBuilder.Entity<OrderUpdate>()
                .HasIndex(o => o.OriginalClientOrderId);
            modelBuilder.Entity<OrderUpdate>()
                .HasIndex(o => new { o.Symbol, o.Side });

            // OrderList
            modelBuilder.Entity<OrderList>()
                .HasKey(o => o.Id);
            modelBuilder.Entity<OrderList>()
                .HasMany(o => o.Orders)
                .WithOne(o => o.OrderList)
                .HasForeignKey(o => o.OrderListId)
                .IsRequired(true);
            modelBuilder.Entity<OrderList>()
                .HasMany(o => o.OrderUpdates)
                .WithOne(o => o.OrderList)
                .HasForeignKey(o => o.OrderListId)
                .IsRequired(false);
            modelBuilder.Entity<OrderList>()
                .HasIndex(o => o.OrderListId);
            modelBuilder.Entity<OrderList>()
                .HasIndex(o => o.Symbol);

            // Order
            modelBuilder.Entity<Order>()
                .HasKey(o => o.Id);
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderUpdates)
                .WithOne(o => o.Order)
                .HasForeignKey(o => o.OrderId)
                .IsRequired(true);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Id);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderId);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderListId);
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.ClientOrderId);

            // Tick
            modelBuilder.Entity<Tick>()
                .HasKey(o => o.Id);
            modelBuilder.Entity<Tick>()
                .HasIndex(o => o.Id);
            modelBuilder.Entity<Tick>()
                .HasIndex(o => new { o.StatisticsOpenTime, o.StatisticsCloseTime });

            // Kline
            modelBuilder.Entity<Kline>()
                .HasKey(o => o.Id);
            modelBuilder.Entity<Kline>()
                .HasIndex(o => o.Id);
            modelBuilder.Entity<Kline>()
                .HasIndex(o => new { o.OpenTime, o.Interval, o.Symbol });

            // Book Tick
            modelBuilder.Entity<BookTick>()
                .HasKey(o => o.UpdateId);
            modelBuilder.Entity<BookTick>()
                .HasIndex(o => o.UpdateId);
            modelBuilder.Entity<BookTick>()
                .HasIndex(o => new { o.CreatedOn, o.Symbol });

            // Recommendation
            modelBuilder.Entity<Recommendation>()
                .HasKey(o => o.Id);
            modelBuilder.Entity<Recommendation>()
                .HasIndex(o => o.Id);
            modelBuilder.Entity<Recommendation>()
                .HasIndex(o => new { o.CreatedOn, o.Symbol });

            // Detailed Order
            modelBuilder.Entity<ClientOrder>()
                .HasKey(o => o.Id);
            modelBuilder.Entity<ClientOrder>()
                .HasIndex(o => o.Id);
            modelBuilder.Entity<ClientOrder>()
                .HasIndex(o => o.Symbol);
            modelBuilder.Entity<ClientOrder>()
                .HasIndex(o => o.CreatedOn);

            // Placed Order
            modelBuilder.Entity<PlacedOrder>()
                .HasKey(o => o.OrderId);
            modelBuilder.Entity<PlacedOrder>()
                .HasIndex(o => o.OrderId);
            modelBuilder.Entity<PlacedOrder>()
                .HasIndex(o => o.OrderListId);
            modelBuilder.Entity<PlacedOrder>()
                .HasIndex(o => o.ClientOrderId);
            modelBuilder.Entity<PlacedOrder>()
                .HasIndex(o => o.OriginalClientOrderId);
            modelBuilder.Entity<PlacedOrder>()
                .HasIndex(o => new { o.TransactionTime, o.Symbol });

            // Order Trade
            modelBuilder.Entity<OrderTrade>()
                .HasKey(o => o.TradeId);
            modelBuilder.Entity<OrderTrade>()
                .HasOne(o => o.PlacedOrder)
                .WithMany(o => o.Fills)
                .HasForeignKey(o => o.PlacedOrderId);
            modelBuilder.Entity<OrderTrade>()
                .HasIndex(o => o.TradeId);
            modelBuilder.Entity<OrderTrade>()
                .HasIndex(o => o.PlacedOrderId);


            base.OnModelCreating(modelBuilder);

            // Fix naming
            this.FixSnakeCaseNames(modelBuilder);
        }

        private void FixSnakeCaseNames(ModelBuilder modelBuilder)
        {
            var mapper = new NpgsqlSnakeCaseNameTranslator();
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // modify column names
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(mapper.TranslateMemberName(property.GetColumnName()));
                }

                // modify table name
                entity.SetTableName(mapper.TranslateMemberName(entity.GetTableName()));
            }
        }

        /// <summary>
        /// Cleans up the <c>BinanceBookTick</c>s
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<int> CleanUpBookTicks(CancellationToken cancellationToken = default)
        {
            var affectedRows = 0;

            try
            {
                // Generate names so in case of refactor they are adjusted automatically
                // And a dependency to to that elements is established
                var mapper = new NpgsqlSnakeCaseNameTranslator();
                var tableName = mapper.TranslateMemberName(nameof(Models.BookTick));
                var columName = mapper.TranslateMemberName(nameof(Models.BookTick.CreatedOn));

                // Generate SQL statement
                var sql = $"DELETE FROM {tableName} WHERE {columName} < NOW() - INTERVAL '24 hours'";

                affectedRows = await this.Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this._logger.Error(e.Message, e);
            }

            return affectedRows;
        }


        public async Task<IEnumerable<Stats3s>> Get3SecondsTrendAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(this.Set<Stats3s>().FromSqlRaw("SELECT * FROM stats_3s();").AsNoTracking().AsEnumerable());

            #region old
            //IDisposable pushedProperty = null;

            //using (var con = new NpgsqlConnection(this.Database.GetDbConnection().ConnectionString))
            //{
            //    using (var com = new NpgsqlCommand("stats_3s", con))
            //    {
            //        try
            //        {
            //            this._logger.Verbose($"Executing {com.CommandText}");

            //            com.CommandType = CommandType.StoredProcedure;

            //            await con.OpenAsync(cancellationToken).ConfigureAwait(false);

            //            using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
            //            {
            //                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            //                {
            //                    // Skip if values are NULL
            //                    if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false)
            //                        )
            //                    {
            //                        continue;
            //                    }

            //                    pushedProperty = LogContext.PushProperty("reader", reader);

            //                    trends.Add(new Stats3s(
            //                        reader.GetString(0),
            //                        reader.GetInt32(1),
            //                        reader.GetDecimal(2),
            //                        reader.GetDecimal(3),
            //                        reader.GetDecimal(4),
            //                        reader.GetDecimal(5),
            //                        reader.GetDecimal(6),
            //                        reader.GetDecimal(7),
            //                        reader.GetDecimal(8),
            //                        reader.GetDecimal(9)));
            //                }
            //            }
            //        }
            //        catch (OperationCanceledException)
            //        {
            //            // nothing
            //        }
            //        catch (PostgresException px)
            //        {
            //            if (px.MessageText != "canceling statement due to user request")
            //            {
            //                this._logger.Fatal(px, px.Message, com.CommandText);
            //            }
            //        }
            //        catch (NpgsqlException ne)
            //        {
            //            this._logger.Fatal(ne, ne.Message, com.CommandText);
            //        }
            //        catch (Exception e)
            //        {
            //            this._logger.Fatal($"General Exception: {e.Message}");
            //        }
            //        finally
            //        {
            //            pushedProperty?.Dispose();

            //            con.Close();
            //        }
            //    }
            //}

            //return trends;
            #endregion
        }

        public async Task<IEnumerable<Stats15s>> Get15SecondsTrendAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(this.Set<Stats15s>().FromSqlRaw("SELECT * FROM stats_15s();").AsNoTracking().AsEnumerable());

            #region old
            //var trends = new List<Stats15s>();
            //IDisposable pushedProperty = null;

            //using (var con = new NpgsqlConnection(this.Database.GetDbConnection().ConnectionString))
            //{
            //    using (var com = new NpgsqlCommand("stats_15s", con))
            //    {
            //        try
            //        {
            //            this._logger.Verbose($"Executing {com.CommandText}");

            //            com.CommandType = CommandType.StoredProcedure;

            //            await con.OpenAsync(cancellationToken).ConfigureAwait(false);

            //            using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
            //            {
            //                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            //                {
            //                    // Skip if values are NULL
            //                    if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false)
            //                        )
            //                    {
            //                        continue;
            //                    }

            //                    pushedProperty = LogContext.PushProperty("reader", reader);

            //                    trends.Add(new Stats15s(
            //                        reader.GetString(0),
            //                        reader.GetInt32(1),
            //                        reader.GetDecimal(2),
            //                        reader.GetDecimal(3),
            //                        reader.GetDecimal(4),
            //                        reader.GetDecimal(5),
            //                        reader.GetDecimal(6),
            //                        reader.GetDecimal(7),
            //                        reader.GetDecimal(8),
            //                        reader.GetDecimal(9)));
            //                }
            //            }
            //        }
            //        catch (OperationCanceledException)
            //        {
            //            // nothing
            //        }
            //        catch (PostgresException px)
            //        {
            //            if (px.MessageText != "canceling statement due to user request")
            //            {
            //                this._logger.Fatal(px, px.Message, com.CommandText);
            //            }
            //        }
            //        catch (NpgsqlException ne)
            //        {
            //            this._logger.Fatal(ne, ne.Message, com.CommandText);
            //        }
            //        catch (Exception e)
            //        {
            //            this._logger.Fatal($"General Exception: {e.Message}");
            //        }
            //        finally
            //        {
            //            pushedProperty?.Dispose();

            //            con.Close();
            //        }
            //    }
            //}

            //return trends;
            #endregion
        }

        public async Task<IEnumerable<Stats2m>> Get2MinutesTrendAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(this.Set<Stats2m>().FromSqlRaw("SELECT * FROM stats_2m();").AsNoTracking().AsEnumerable());

            #region old
            //var trends = new List<Stats2m>();
            //IDisposable pushedProperty = null;

            //using (var con = new NpgsqlConnection(this.Database.GetDbConnection().ConnectionString))
            //{
            //    using (var com = new NpgsqlCommand("stats_2m", con))
            //    {
            //        try
            //        {
            //            this._logger.Verbose($"Executing {com.CommandText}");

            //            com.CommandType = CommandType.StoredProcedure;

            //            await con.OpenAsync(cancellationToken).ConfigureAwait(false);

            //            using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
            //            {
            //                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            //                {
            //                    // Skip if values are NULL
            //                    if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false)
            //                        )
            //                    {
            //                        continue;
            //                    }

            //                    pushedProperty = LogContext.PushProperty("reader", reader);

            //                    trends.Add(new Stats2m(
            //                        reader.GetString(0),
            //                        reader.GetInt32(1),
            //                        reader.GetDecimal(2),
            //                        reader.GetDecimal(3),
            //                        reader.GetDecimal(4),
            //                        reader.GetDecimal(5),
            //                        reader.GetDecimal(6),
            //                        reader.GetDecimal(7),
            //                        reader.GetDecimal(8),
            //                        reader.GetDecimal(9)));
            //                }
            //            }
            //        }
            //        catch (OperationCanceledException)
            //        {
            //            // nothing
            //        }
            //        catch (PostgresException px)
            //        {
            //            if (px.MessageText != "canceling statement due to user request")
            //            {
            //                this._logger.Fatal(px, px.Message, com.CommandText);
            //            }
            //        }
            //        catch (NpgsqlException ne)
            //        {
            //            this._logger.Fatal(ne, ne.Message, com.CommandText);
            //        }
            //        catch (Exception e)
            //        {
            //            this._logger.Fatal($"General Exception: {e.Message}");
            //        }
            //        finally
            //        {
            //            pushedProperty?.Dispose();

            //            con.Close();
            //        }
            //    }
            //}

            //return trends;
            #endregion
        }

        public async Task<IEnumerable<Stats10m>> Get10MinutesTrendAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(this.Set<Stats10m>().FromSqlRaw("SELECT * FROM stats_10m();").AsNoTracking().AsEnumerable());

            #region old
            //var trends = new List<Stats10m>();
            //IDisposable pushedProperty = null;

            //using (var con = new NpgsqlConnection(this.Database.GetDbConnection().ConnectionString))
            //{
            //    using (var com = new NpgsqlCommand("stats_10m", con))
            //    {
            //        try
            //        {
            //            this._logger.Verbose($"Executing {com.CommandText}");

            //            com.CommandType = CommandType.StoredProcedure;

            //            await con.OpenAsync(cancellationToken).ConfigureAwait(false);

            //            using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
            //            {
            //                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            //                {
            //                    // Skip if values are NULL
            //                    if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false)
            //                        )
            //                    {
            //                        continue;
            //                    }

            //                    pushedProperty = LogContext.PushProperty("reader", reader);

            //                    trends.Add(new Stats10m(
            //                        reader.GetString(0),
            //                        reader.GetInt32(1),
            //                        reader.GetDecimal(2),
            //                        reader.GetDecimal(3),
            //                        reader.GetDecimal(4),
            //                        reader.GetDecimal(5),
            //                        reader.GetDecimal(6),
            //                        reader.GetDecimal(7),
            //                        reader.GetDecimal(8),
            //                        reader.GetDecimal(9)));
            //                }
            //            }
            //        }
            //        catch (OperationCanceledException)
            //        {
            //            // nothing
            //        }
            //        catch (PostgresException px)
            //        {
            //            if (px.MessageText != "canceling statement due to user request")
            //            {
            //                this._logger.Fatal(px, px.Message, com.CommandText);
            //            }
            //        }
            //        catch (NpgsqlException ne)
            //        {
            //            this._logger.Fatal(ne, ne.Message, com.CommandText);
            //        }
            //        catch (Exception e)
            //        {
            //            this._logger.Fatal($"General Exception: {e.Message}");
            //        }
            //        finally
            //        {
            //            pushedProperty?.Dispose();

            //            con.Close();
            //        }
            //    }
            //}

            //return trends;
            #endregion
        }

        public async Task<IEnumerable<Stats2h>> Get2HoursTrendAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(this.Set<Stats2h>().FromSqlRaw("SELECT * FROM stats_2h();").AsNoTracking().AsEnumerable());

            #region old
            //var trends = new List<Stats2h>();
            //IDisposable pushedProperty = null;

            //using (var con = new NpgsqlConnection(this.Database.GetDbConnection().ConnectionString))
            //{
            //    using (var com = new NpgsqlCommand("stats_2h", con))
            //    {
            //        try
            //        {
            //            this._logger.Verbose($"Executing {com.CommandText}");

            //            com.CommandType = CommandType.StoredProcedure;

            //            await con.OpenAsync(cancellationToken).ConfigureAwait(false);

            //            using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
            //            {
            //                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            //                {
            //                    // Skip if values are NULL
            //                    if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false)
            //                        )
            //                    {
            //                        continue;
            //                    }

            //                    pushedProperty = LogContext.PushProperty("reader", reader);

            //                    trends.Add(new Stats2h(
            //                        reader.GetString(0),
            //                        reader.GetInt32(1),
            //                        reader.GetDecimal(2),
            //                        reader.GetDecimal(3),
            //                        reader.GetDecimal(4),
            //                        reader.GetDecimal(5),
            //                        reader.GetDecimal(6),
            //                        reader.GetDecimal(7),
            //                        reader.GetDecimal(8),
            //                        reader.GetDecimal(9)));
            //                }
            //            }
            //        }
            //        catch (OperationCanceledException)
            //        {
            //            // nothing
            //        }
            //        catch (PostgresException px)
            //        {
            //            if (px.MessageText != "canceling statement due to user request")
            //            {
            //                this._logger.Fatal(px, px.Message, com.CommandText);
            //            }
            //        }
            //        catch (NpgsqlException ne)
            //        {
            //            this._logger.Fatal(ne, ne.Message, com.CommandText);
            //        }
            //        catch (Exception e)
            //        {
            //            this._logger.Fatal($"General Exception: {e.Message}");
            //        }
            //        finally
            //        {
            //            pushedProperty?.Dispose();

            //            con.Close();
            //        }
            //    }
            //}

            //return trends;
            #endregion
        }

        public async Task<IEnumerable<LatestMA10mAndMA30mCrossing>> GetLatestMA10mAndMA30mCrossing(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(this.Set<LatestMA10mAndMA30mCrossing>().FromSqlRaw("SELECT * FROM get_latest_ma10m_ma30m_crossing();").AsNoTracking().AsEnumerable());

            #region old

            //var latestCrossings = new List<LatestMA10mAndMA30mCrossing>();
            //IDisposable pushedProperty = null;

            //using (var con = new NpgsqlConnection(this.Database.GetDbConnection().ConnectionString))
            //{
            //    using (var com = new NpgsqlCommand("get_latest_ma10m_ma30m_crossing", con))
            //    {
            //        try
            //        {
            //            this._logger.Verbose($"Executing {com.CommandText}");

            //            com.CommandType = CommandType.StoredProcedure;

            //            await con.OpenAsync(cancellationToken).ConfigureAwait(false);

            //            using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
            //            {
            //                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            //                {
            //                    pushedProperty = LogContext.PushProperty("reader", reader);

            //                    // Skip if values are NULL
            //                    if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false))
            //                    {
            //                        continue;
            //                    }

            //                    latestCrossings.Add(new LatestMA10mAndMA30mCrossing(
            //                        reader.GetString(0),
            //                        reader.GetDateTime(1),
            //                        reader.GetDecimal(2),
            //                        reader.GetDecimal(3)));
            //                }
            //            }
            //        }
            //        catch (OperationCanceledException)
            //        {
            //            // nothing
            //        }
            //        catch (PostgresException px)
            //        {
            //            if (px.MessageText != "canceling statement due to user request")
            //            {
            //                this._logger.Fatal(px, px.Message, com.CommandText);
            //            }
            //        }
            //        catch (NpgsqlException ne)
            //        {
            //            this._logger.Fatal(ne, ne.Message, com.CommandText);
            //        }
            //        catch (Exception e)
            //        {
            //            this._logger.Fatal($"General Exception: {e.Message}");
            //        }
            //        finally
            //        {
            //            pushedProperty?.Dispose();

            //            con.Close();
            //        }
            //    }
            //}

            //return latestCrossings;
            #endregion
        }

        public async Task<IEnumerable<LatestMA30mAndMA1hCrossing>> GetLatestMA30mAndMA1hCrossing(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(this.Set<LatestMA30mAndMA1hCrossing>().FromSqlRaw("SELECT * FROM get_latest_ma30m_ma1h_crossing();").AsNoTracking().AsEnumerable());

            #region old
            //var latestCrossings = new List<LatestMA30mAndMA1hCrossing>();
            //IDisposable pushedProperty = null;

            //using (var con = new NpgsqlConnection(this.Database.GetDbConnection().ConnectionString))
            //{
            //    using (var com = new NpgsqlCommand("get_latest_ma30m_ma1h_crossing", con))
            //    {
            //        try
            //        {
            //            this._logger.Verbose($"Executing {com.CommandText}");

            //            com.CommandType = CommandType.StoredProcedure;

            //            await con.OpenAsync(cancellationToken).ConfigureAwait(false);

            //            using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
            //            {
            //                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            //                {
            //                    pushedProperty = LogContext.PushProperty("reader", reader);

            //                    // Skip if values are NULL
            //                    if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
            //                        )
            //                    {
            //                        continue;
            //                    }

            //                    latestCrossings.Add(new LatestMA30mAndMA1hCrossing(
            //                        reader.GetString(0),
            //                        reader.GetDateTime(1),
            //                        reader.GetDecimal(2),
            //                        reader.GetDecimal(3)));
            //                }
            //            }
            //        }
            //        catch (OperationCanceledException)
            //        {
            //            // nothing
            //        }
            //        catch (PostgresException px)
            //        {
            //            if (px.MessageText != "canceling statement due to user request")
            //            {
            //                this._logger.Fatal(px, px.Message, com.CommandText);
            //            }
            //        }
            //        catch (NpgsqlException ne)
            //        {
            //            this._logger.Fatal(ne, ne.Message, com.CommandText);
            //        }
            //        catch (Exception e)
            //        {
            //            this._logger.Fatal($"General Exception: {e.Message}");
            //        }
            //        finally
            //        {
            //            pushedProperty?.Dispose();

            //            con.Close();
            //        }
            //    }
            //}

            //return latestCrossings;
            #endregion
        }

        public async Task<IEnumerable<LatestMA1hAndMA3hCrossing>> GetLatestMA1hAndMA3hCrossing(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(this.Set<LatestMA1hAndMA3hCrossing>().FromSqlRaw("SELECT * FROM get_latest_ma1h_ma3h_crossing();").AsNoTracking().AsEnumerable());

            #region old
            //var latestCrossings = new List<LatestMA1hAndMA3hCrossing>();
            //IDisposable pushedProperty = null;

            //using (var con = new NpgsqlConnection(this.Database.GetDbConnection().ConnectionString))
            //{
            //    using (var com = new NpgsqlCommand("get_latest_ma1h_ma3h_crossing", con))
            //    {
            //        try
            //        {
            //            this._logger.Verbose($"Executing {com.CommandText}");

            //            com.CommandType = CommandType.StoredProcedure;

            //            await con.OpenAsync(cancellationToken).ConfigureAwait(false);

            //            using (var reader = await com.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false))
            //            {
            //                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            //                {
            //                    pushedProperty = LogContext.PushProperty("reader", reader);

            //                    // Skip if values are NULL
            //                    if (await reader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
            //                        || await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false)
            //                        )
            //                    {
            //                        continue;
            //                    }

            //                    latestCrossings.Add(new LatestMA1hAndMA3hCrossing(
            //                        reader.GetString(0),
            //                        reader.GetDateTime(1),
            //                        reader.GetDecimal(2),
            //                        reader.GetDecimal(3)));
            //                }
            //            }
            //        }
            //        catch (OperationCanceledException)
            //        {
            //            // nothing
            //        }
            //        catch (PostgresException px)
            //        {
            //            if (px.MessageText != "canceling statement due to user request")
            //            {
            //                this._logger.Fatal(px, px.Message, com.CommandText);
            //            }
            //        }
            //        catch (NpgsqlException ne)
            //        {
            //            this._logger.Fatal(ne, ne.Message, com.CommandText);
            //        }
            //        catch (Exception e)
            //        {
            //            this._logger.Fatal($"General Exception: {e.Message}");
            //        }
            //        finally
            //        {
            //            pushedProperty?.Dispose();

            //            con.Close();
            //        }
            //    }
            //}

            //return latestCrossings;
            #endregion
        }


        public async Task<decimal> GetPriceOn(string symbol, DateTime dateTime, CancellationToken cancellationToken = default)
        {
            var price = default(decimal);
            IDisposable pushedProperty = null;

            using (var con = new NpgsqlConnection(this.Database.GetDbConnection().ConnectionString))
            {
                using (var com = new NpgsqlCommand("get_price_on", con))
                {
                    try
                    {
                        this._logger.Verbose($"Executing {com.CommandText}");

                        com.CommandType = CommandType.StoredProcedure;

                        com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = symbol;
                        com.Parameters.Add("p_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = dateTime.ToUniversalTime();

                        await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                        var obj = await com.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

                        pushedProperty = LogContext.PushProperty("obj", obj);

                        price = (decimal)obj;
                    }
                    catch (InvalidCastException)
                    {
                        this._logger.Error("Value is not decimal");
                    }
                    catch (OperationCanceledException)
                    {
                        // nothing
                    }
                    catch (PostgresException px)
                    {
                        if (px.MessageText != "canceling statement due to user request")
                        {
                            this._logger.Fatal(px, px.Message, com.CommandText);
                        }
                    }
                    catch (NpgsqlException ne)
                    {
                        this._logger.Fatal(ne, ne.Message, com.CommandText);
                    }
                    catch (Exception e)
                    {
                        this._logger.Fatal($"General Exception: {e.Message}");
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
    }
}
