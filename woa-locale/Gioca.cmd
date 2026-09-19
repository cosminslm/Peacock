@echo off
title HITMAN WOA - solo Peacock locale (niente download)
cd /d "%~dp0"
echo.
echo  Usa Peacock GIA sul disco. Nessun download.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Launch-Locale.ps1"
if errorlevel 1 pause
