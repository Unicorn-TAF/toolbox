@echo off

set VERSION=%1
set OUT_DIR=%2\unicorn-toolbox
set PROJ_PATH=%cd%\..\src\Unicorn.Toolbox\Unicorn.Toolbox.csproj

for %%v in (net462, net6.0-windows) do (
	dotnet publish %PROJ_PATH% --configuration Release --framework %%v --output %OUT_DIR%\%%v -p:VersionPrefix=%VERSION%
	del %OUT_DIR%\%%v\AspectInjector.Broker.dll
)

