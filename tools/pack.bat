@echo off
set SRC_ROOT=%cd%\..\src
set PKG=%1
set OUT_DIR=%2

dotnet pack %SRC_ROOT%\%PKG%\%PKG%.csproj -o %OUT_DIR% -c Release -p:NuspecFile=%SRC_ROOT%\%PKG%\%PKG%.nuspec