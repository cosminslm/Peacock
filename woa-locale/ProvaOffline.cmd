@echo off
title HITMAN WOA - PROVA COMPLETAMENTE OFFLINE
cd /d "%~dp0"
echo.
echo  1. Spegni Wi-Fi e stacca il cavo (aereo ON).
echo  2. Chiudi HITMAN e ogni Peacock Server.
echo  3. Questo script si ferma se vede ancora internet.
echo.
pause
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Launch-Locale.ps1" -RequireOffline
if errorlevel 1 pause
