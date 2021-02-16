# Trape - Trading Ape

This is just a fun project to develop a trading bot that connects to Binance (https://binance.com) and trades asset using USDT.
It was developed with mainly BTC/USDT in mind. So other assets/USDT (especially with a different value (lower or higher than around 13000 USDT per token might cause unexpected behavior, focus was on BTC))

# How does it work?
Trape.Trader connects to Binance and receives KLineDiagrams and ExchangeInfos.
It compares the current vs the new ExchangeInfo and checks if there is a new asset/USDT.
If so, is spawns an instance of Analyst and one of Broker which are connected via Reactive Extensions.
Then the Analyst should instruct the Broker to buy and to sell a bit later.
The idea is to make quick wins with assets that join the platform and that will peak for some minutes before normal trading of the asset starts. Of course an asset can also drop drastically after being purchased, so ... there may be losses. However, this project is more about playing with Reactive extensions.
Compared to the previous version of Trape the biggest change is the way trading behaves and also that the need for a database was completely removed.

## Technologies
- .NET Core 5.0
- Binance.Net
- Serilog
- Simpleinjector

## Build
Build Scripts to build on Windows and Linux (with .deb packaging) are also provided.
On Linux, the resulting RPM also contains systemd configuration to run Trape as a service.

## Installation
1. Install both generated .deb package on Ubuntu.
1. Generate a binance API key / secret key in your account.
1. Copy /opt/trape/*/settings.template.json to settings.json and modify content
