@echo off
title Peacock - profilo locale
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0ProfiloLocale.ps1"
if errorlevel 1 pause
