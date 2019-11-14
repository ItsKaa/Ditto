@ECHO off
@TITLE KaaBot
REM cd /D "..\Ditto.Bot"
cd /D "..\Ditto.Bot"
dotnet run --configuration Release
TITLE KaaBot - Stopped
CD /D "%~dp0"
PAUSE >nul 2>&1