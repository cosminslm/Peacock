@echo off
title HITMAN WOA - Peacock Offline
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Launch-WOA-Offline.ps1"
if errorlevel 1 pause
