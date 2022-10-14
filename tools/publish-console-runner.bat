@echo off

set OUT_DIR=%1

cd %cd%\..\src\Unicorn.ConsoleRunner

dotnet publish -c release -f net452 --output %OUT_DIR%\unicorn-console\net452
dotnet publish -c release -f net5.0 --output %OUT_DIR%\unicorn-console\net5.0
dotnet publish -c release -f net5.0 -r linux-x64 --no-self-contained --output %OUT_DIR%\unicorn-console\linux-x64
dotnet publish -c release -f net5.0 -r linux-arm --no-self-contained --output %OUT_DIR%\unicorn-console\linux-arm