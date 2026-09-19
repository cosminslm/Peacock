@echo off
title Ripristino assetto originale Peacock (C:\Games)
set "PK=C:\Games\HITMAN - World of Assassination\Peacock"
set "U=%PK%\userdata\users"
set "J=%U%\dbb3dd5d-5df5-469c-9e35-271fa86815d8.json"

echo.
echo  Chiudi HITMAN, PeacockPatcher e le finestre nere del server, poi premi un tasto.
pause >nul
taskkill /F /IM PeacockPatcher.exe >nul 2>&1
taskkill /F /IM node.exe >nul 2>&1

if not exist "%PK%\chunk0.js" (
  echo [ERRORE] Non trovo Peacock in %PK%
  pause
  exit /b 1
)

echo.
echo  1. Profilo: rimetto quello del 17/09 (prima delle modifiche)
if exist "%J%.bak" (
  copy /Y "%J%" "%J%.modificato" >nul
  copy /Y "%J%.bak" "%J%" >nul && echo     OK
) else (
  echo     nessun .bak: lascio il file com'e'
)

echo  2. Tolgo i file LEGGIMI aggiunti da me
del /Q "%U%\*.LEGGIMI.txt" >nul 2>&1
echo     OK

echo  3. options.ini: torna a quello di fabbrica (Peacock lo ricrea da solo)
if exist "%PK%\options.ini" move /Y "%PK%\options.ini" "%PK%\options.ini.modificato" >nul
echo     OK

echo.
echo  FATTO. Da ora NON usare piu' Gioca.cmd / ProvaOffline.cmd.
echo  Avvia come facevi all'inizio, in QUESTO ordine:
echo    1. "%PK%\Start Server.cmd"   (aspetta che dica che e' in ascolto)
echo    2. "%PK%\PeacockPatcher.exe" (aspetta che sia aperto)
echo    3. HITMAN3.exe               (solo dopo, quando il Patcher e' gia' su)
echo.
pause
