@echo off
title Peacock - ripristina profilo dal .bak
cd /d "%~dp0"
echo Chiudi HITMAN e Peacock Server prima di continuare.
pause
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0RipristinaProfilo.ps1"
if errorlevel 1 pause
