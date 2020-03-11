cd C:\Projekte\damian\trape\src\trape\trape.cli.collector
dotnet build -c Debug --runtime ubuntu.18.04-x64
dotnet build -c Release --runtime ubuntu.18.04-x64

cd C:\Projekte\damian\trape\src\trape\trape.cli.trader
dotnet build -c Debug --runtime ubuntu.18.04-x64
dotnet build -c Release --runtime ubuntu.18.04-x64
