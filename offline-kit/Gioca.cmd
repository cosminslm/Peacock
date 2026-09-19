@echo off
title HITMAN WOA - Peacock (unico avvio)
cd /d "%~dp0"
echo.
echo  Unico file da usare. Si auto-eleva ad amministratore.
echo  La release Peacock si scarica SOLO se non e gia installata.
echo  I progressi in userdata NON vengono cancellati.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Launch-WOA-Offline.ps1"
if errorlevel 1 pause
