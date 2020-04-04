@echo off
cd C:\Projekte\damian\trape\src\trape\trape.cli.collector
@echo on
dotnet clean -c Debug --runtime ubuntu.18.04-x64
dotnet clean -c Release --runtime ubuntu.18.04-x64
dotnet build -c Debug --runtime ubuntu.18.04-x64
dotnet build -c Release --runtime ubuntu.18.04-x64

@echo off
cd C:\Projekte\damian\trape\src\trape\trape.cli.trader
@echo on
dotnet clean -c Debug --runtime ubuntu.18.04-x64
dotnet clean -c Release --runtime ubuntu.18.04-x64
dotnet build -c Debug --runtime ubuntu.18.04-x64
dotnet build -c Release --runtime ubuntu.18.04-x64

cd C:\Projekte\damian\trape\src

scp trape\trape.cli.trader\bin\Debug\netcoreapp3.1\ubuntu.18.04-x64\* damian@universe:~/trape.trader/
scp trape\trape.cli.collector\bin\Debug\netcoreapp3.1\ubuntu.18.04-x64\* damian@universe:~/trape.collector/
