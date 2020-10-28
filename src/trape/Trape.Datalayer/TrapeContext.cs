using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Npgsql.NameTranslation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using trape.datalayer.Models;

namespace trape.datalayer
{
    /// <summary>
    /// Trape database context
    /// </summary>
    public class TrapeContext : DbContext
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <c>TrapeContext</c> class.
        /// </summary>
        /// <param name="options"></param>
        public TrapeContext(DbContextOptions<TrapeContext> options)
            : base(options)
        {

        }

        #endregion

        #region Properties

        /// <summary>
        /// Account information
        /// </summary>
        public DbSet<AccountInfo> AccountInfos { get; set; }

        /// <summary>
        /// Balances
        /// </summary>
        public DbSet<Balance> Balances { get; set; }

        /// <summary>
        /// Balance updates
        /// </summary>
        public DbSet<BalanceUpdate> BalanceUpdates { get; set; }

        /// <summary>
        /// Book ticks
        /// </summary>
        public DbSet<BookPrice> BookPrices { get; set; }

        /// <summary>
        /// Orders
        /// </summary>
        public DbSet<Order> Orders { get; set; }

        /// <summary>
        /// Order lists
        /// </summary>
        public DbSet<OrderList> OrderLists { get; set; }

        /// <summary>
        /// Order updates
        /// </summary>
        public DbSet<OrderUpdate> OrderUpdates { get; set; }

        /// <summary>
        /// Ticks
        /// </summary>
        public DbSet<Tick> Ticks { get; set; }

        /// <summary>
        /// Kline charts
        /// </summary>
        public DbSet<Kline> Klines { get; set; }

        /// <summary>
        /// Symbols
        /// </summary>
        public DbSet<Symbol> Symbols { get; set; }

        /// <summary>
        /// Recommendations
        /// </summary>
        public DbSet<Recommendation> Recommendations { get; set; }

        /// <summary>
        /// Custom order format
        /// </summary>
        public DbSet<ClientOrder> ClientOrder { get; set; }

        /// <summary>
        /// Placed orders
        /// </summary>
        public DbSet<PlacedOrder> PlacedOrders { get; set; }

        /// <summary>
        /// Trades per order
        /// </summary>
        public DbSet<OrderTrade> OrderTrades { get; set; }

        #endregion

        #region DbContext Methods

        /// <summary>
        /// Override of <see cref="OnConfiguring(DbContextOptionsBuilder)"/>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => base.OnConfiguring(optionsBuilder);

        /// <summary>
        /// Override of <see cref="OnModelCreating(ModelBuilder)"/>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder is null)
            {
                return;
            }

            #region Stats

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
            modelBuilder.Entity<LastDecision>()
                .HasNoKey();

            #endregion

            #region Models

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
                .HasIndex(o => new { o.OpenTime, o.CloseTime });
            modelBuilder.Entity<Tick>()
                .HasIndex(o => o.Id);

            // Kline
            modelBuilder.Entity<Kline>()
                .HasKey(o => o.Id);
            modelBuilder.Entity<Kline>()
                .HasIndex(o => new { o.OpenTime, o.Interval, o.Symbol });
            modelBuilder.Entity<Kline>()
                .HasIndex(o => o.Id);

            // Book Tick
            modelBuilder.Entity<BookPrice>()
                .HasKey(o => o.UpdateId);
            modelBuilder.Entity<BookPrice>()
                .HasIndex(o => o.UpdateId);
            modelBuilder.Entity<BookPrice>()
                .HasIndex(o => new { o.CreatedOn, o.Symbol });

            // Recommendation
            modelBuilder.Entity<Recommendation>()
                .HasKey(o => o.Id);
            modelBuilder.Entity<Recommendation>()
                .HasIndex(o => o.Id);

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

            #endregion

            base.OnModelCreating(modelBuilder);

            // Fix naming
            FixSnakeCaseNames(modelBuilder);
        }

        /// <summary>
        /// Fixes the naming to create proper snake case names
        /// </summary>
        /// <param name="modelBuilder"></param>
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

        #endregion

        /// <summary>
        /// Cleans up the <c>BinanceBookTick</c>s
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<int> CleanUpBookTicks(CancellationToken cancellationToken = default)
        {
            // Generate names so in case of refactor they are adjusted automatically
            // And a dependency to to that elements is established
            var mapper = new NpgsqlSnakeCaseNameTranslator();
            var tableName = mapper.TranslateMemberName(nameof(BookPrices));
            var columName = mapper.TranslateMemberName(nameof(BookPrice.CreatedOn));

            // Generate SQL statement
            var sql = $"DELETE FROM {tableName} WHERE {columName} < NOW() - INTERVAL '24 hours'";

            return await Database.ExecuteSqlRawAsync(sql, cancellationToken).ConfigureAwait(false);
        }


        public Task<List<Stats3s>> Get3SecondsTrendAsync()
        {
            return Set<Stats3s>().FromSqlRaw("SELECT * FROM stats_3s();").AsNoTracking().ToListAsync();
        }

        public Task<List<Stats15s>> Get15SecondsTrendAsync()
        {
            return Set<Stats15s>().FromSqlRaw("SELECT * FROM stats_15s();").AsNoTracking().ToListAsync();
        }

        public Task<List<Stats2m>> Get2MinutesTrendAsync()
        {
            return Set<Stats2m>().FromSqlRaw("SELECT * FROM stats_2m();").AsNoTracking().ToListAsync();
        }

        public Task<List<Stats10m>> Get10MinutesTrendAsync()
        {
            return Set<Stats10m>().FromSqlRaw("SELECT * FROM stats_10m();").AsNoTracking().ToListAsync();
        }

        public Task<List<Stats2h>> Get2HoursTrendAsync()
        {
            return Set<Stats2h>().FromSqlRaw("SELECT * FROM stats_2h();").AsNoTracking().ToListAsync();
        }

        public Task<List<LatestMA10mAndMA30mCrossing>> GetLatestMA10mAndMA30mCrossing()
        {
            return Set<LatestMA10mAndMA30mCrossing>().FromSqlRaw("SELECT * FROM get_latest_ma10m_ma30m_crossing();").ToListAsync();
        }

        public Task<List<LatestMA30mAndMA1hCrossing>> GetLatestMA30mAndMA1hCrossing()
        {
            return Set<LatestMA30mAndMA1hCrossing>().FromSqlRaw("SELECT * FROM get_latest_ma30m_ma1h_crossing();").AsNoTracking().ToListAsync();
        }

        public Task<List<LatestMA1hAndMA3hCrossing>> GetLatestMA1hAndMA3hCrossing()
        {
            return Set<LatestMA1hAndMA3hCrossing>().FromSqlRaw("SELECT * FROM get_latest_ma1h_ma3h_crossing();").AsNoTracking().ToListAsync();
        }

        public Task<List<LastDecision>> GetLastDecisions()
        {
            return Set<LastDecision>().FromSqlRaw("SELECT * FROM get_last_decisions();").AsNoTracking().ToListAsync();
        }

        public async Task<decimal> GetLowestPrice(string symbol, DateTime dateTime, CancellationToken cancellationToken = default)
        {
            var price = default(decimal);

            using (var con = new NpgsqlConnection(Database.GetDbConnection().ConnectionString))
            {
                using var com = new NpgsqlCommand("get_lowest_price", con);
                try
                {
                    com.CommandType = CommandType.StoredProcedure;

                    com.Parameters.Add("p_symbol", NpgsqlTypes.NpgsqlDbType.Text).Value = symbol;
                    com.Parameters.Add("p_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = dateTime.ToUniversalTime();

                    await con.OpenAsync(cancellationToken).ConfigureAwait(false);

                    var obj = await com.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

                    price = (decimal)obj;
                }
                catch
                {
                    // rethrow
                    throw;
                }
                finally
                {
                    con.Close();
                }
            }

            return price;
        }
    }
}
