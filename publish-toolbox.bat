@echo off

cd src\Unicorn.Toolbox

dotnet publish -c release --framework net452
dotnet publish -c release --framework net5.0-windows