@ECHO off
@TITLE KaaBot
cd /D "..\Kaa.Discord"
dotnet run --configuration Release
TITLE KaaBot - Stopped
CD /D "%~dp0"
PAUSE >nul 2>&1