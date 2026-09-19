@echo off
chcp 65001 >nul
title Ripristino profilo Peacock (dal .bak)
echo.
echo  Chiudi HITMAN, Patcher e Peacock Server, poi premi un tasto.
pause >nul

set "USERS=%USERPROFILE%\Documents\Peacock\userdata\users"
set "JSON=%USERS%\dbb3dd5d-5df5-469c-9e35-271fa86815d8.json"
set "BAK=%JSON%.bak"

if not exist "%BAK%" (
  echo [ERRORE] Non trovo il backup:
  echo   %BAK%
  pause
  exit /b 1
)

echo.
echo  Sorgente  (progressi):
dir /n "%BAK%" | findstr /i "dbb3dd5d"
echo  Destinazione (verra sostituito):
if exist "%JSON%" dir /n "%JSON%" | findstr /i "dbb3dd5d"

if exist "%JSON%" copy /Y "%JSON%" "%JSON%.VUOTO" >nul
copy /Y "%BAK%" "%JSON%"
if errorlevel 1 (
  echo [ERRORE] Copia fallita.
  pause
  exit /b 1
)

echo.
echo  [OK] Profilo ripristinato. Deve pesare circa 38695 byte:
dir /n "%JSON%" | findstr /i "dbb3dd5d"
echo.
echo  Da ora usa SOLO:
echo    %USERPROFILE%\Documents\Peacock
echo  NON usare C:\Games\HITMAN...\Peacock
echo.
echo  Poi: Gioca.cmd ^(tasto destro, amministratore^).
echo  Hub - Planning - Start. Niente Continua.
echo.
pause
