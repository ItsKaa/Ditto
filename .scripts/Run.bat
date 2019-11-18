@ECHO off
@TITLE Ditto
cd /D "..\Ditto.Bot"
dotnet run --configuration Release
TITLE Ditto - Stopped
CD /D "%~dp0"
PAUSE >nul 2>&1