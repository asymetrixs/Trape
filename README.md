# Trape - Trading Ape

This is just a fun project to develop a trading bot that connects to Binance (https://binance.com) and trades asset using USDT.

# How does it work?
Trape has two main projects, Trape.Collector and Trape.Trader.
The Collector connects to Binance and receives trading information (book price, KLines, etc.) for certain assets. It saves these information in the local PostgreSQL database.
The Collector is independent from the Trader so that the Trader can be redeployed (taken offline) without loosing information from Binance.

The Trader uses historical information from the database and also live information from Binance to generate recommendations about when it is advised to buy or sell. The Trader also uses this information to perform the actual buy/sell.
It looks at 5-15 minute intervals.

## Web frontent
I started with a web frontend but haven't but any more effort into it due to time constraints.

## Technologies
- .NET Core 3.1
- Binance.Net
- Entity Framework
- Npgsql
- Serilog
- Simpleinjector
- TimescaleDB (in PostgreSQL)

# Build
Build Scripts to build on Windows and Linux (with .deb packaging) are also provided.
On Linux, the resulting RPM also contains systemd configuration to run Trape as a service.