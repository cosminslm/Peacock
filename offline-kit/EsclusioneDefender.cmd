@echo off
title Peacock - Esclusione Defender
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Add-DefenderExclusion.ps1"
if errorlevel 1 pause
