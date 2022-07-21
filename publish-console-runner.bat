@echo off

cd src\Unicorn.ConsoleRunner

dotnet publish -c release --framework net452
dotnet publish -c release --framework net5.0