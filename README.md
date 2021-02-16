# Trape - Trading Ape

This is just a fun project to develop a trading bot that connects to Binance (https://binance.com) and trades asset using USDT.
It was developed with mainly BTC/USDT in mind. So other assets/USDT (especially with a different value (lower or higher than around 13000 USDT per token might cause unexpected behavior, focus was on BTC))

# How does it work?
Trape has two main projects, Trape.Collector and Trape.Trader.
The Collector connects to Binance and receives trading information (book price, KLines, etc.) for certain assets. It saves these information in the local PostgreSQL database.
The Collector is independent from the Trader so that the Trader can be redeployed (taken offline) without loosing information from Binance.

The Trader uses historical information from the database and also live information from Binance to generate recommendations about when it is advised to buy or sell. The Trader also uses this information to perform the actual buy/sell.
It looks at 5-15 minute intervals.

## Web Frontend
I started with a web frontend but haven't put any more effort into it due to time constraints.

## Technologies
- .NET Core 3.1
- Binance.Net
- Entity Framework
- Npgsql
- Serilog
- Simpleinjector
- TimescaleDB (in PostgreSQL)

## Build
Build Scripts to build on Windows and Linux (with .deb packaging) are also provided.
On Linux, the resulting RPM also contains systemd configuration to run Trape as a service.

## Installation
1. Install both generated .deb package on Ubuntu.
1. Generate a binance API key / secret key in your account.
1. Create a database and a user in PostgreSQL
1. Copy /opt/trape/*/settings.template.json to settings.json and modify content
1. Install TimescaleDB in PostgreSQL https://docs.timescale.com/latest/getting-started/installation
1. Run `dotnet ef database update --project ./Trape.Datalayer/ --startup-project ./Trape.Cli.Collector/`
1. Modify tables to support TimescaleDB and add missing SQL functions by running Trape.Datalayer/sql-setup.sql on the database.
1. Finally, add assets. Only asset/USDT will work at the moment. Also, I am not sure how it behaves with USDT asset values other than around 13000 USDT per token.
```
INSERT INTO symbols(name, is_collection_active, is_trading_active) VALUES('BTCUSDT', true, false);
INSERT INTO symbols(name, is_collection_active, is_trading_active) VALUES('ETHUSDT', false, false);
INSERT INTO symbols(name, is_collection_active, is_trading_active) VALUES('LINKUSDT', false, false);
```
