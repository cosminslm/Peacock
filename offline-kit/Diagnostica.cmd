@echo off
title Peacock - Diagnostica
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Diagnose-Peacock.ps1"
