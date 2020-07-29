@echo off
cd C:\Projects\damian\trape\src\trape\trape.cli.collector

rmdir /S /Q bin\Debug
rmdir /S /Q bin\Release

@echo on
dotnet build --configuration:Debug --runtime:ubuntu.18.04-x64
dotnet build --configuration:Release --runtime:ubuntu.18.04-x64

cd C:\Projects\damian\trape\src

scp trape\trape.cli.collector\bin\Debug\netcoreapp3.1\ubuntu.18.04-x64\* damian@universe:~/trape.collector/
