@echo off
title Peacock - dopo la sfilata (Parigi)
cd /d "%~dp0"
echo Chiudi HITMAN e la finestra Peacock Server prima di continuare.
pause
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Skip-ToAfterShowstopper.ps1"
if errorlevel 1 pause
