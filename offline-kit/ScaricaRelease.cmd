@echo off
title Peacock - Scarica release ufficiale
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Get-OfficialRelease.ps1"
if errorlevel 1 pause
