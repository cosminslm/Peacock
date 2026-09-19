@echo off
title Diagnosi Peacock (solo lettura)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Diagnosi.ps1"
if errorlevel 1 pause
