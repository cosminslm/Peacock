@echo off
setlocal EnableDelayedExpansion
title Diagnosi Peacock (solo lettura, nessuna modifica)

set "OUT=%USERPROFILE%\Desktop\peacock-diagnosi.txt"
set "PK=%USERPROFILE%\Documents\Peacock"
set "PK2=C:\Games\HITMAN - World of Assassination\Peacock"
set "GAMEDIR=C:\Games\HITMAN - World of Assassination\Retail"

> "%OUT%" echo === DIAGNOSI PEACOCK  %DATE% %TIME% ===
>> "%OUT%" echo.

>> "%OUT%" echo === Rete (internet raggiungibile?) ===
ping -n 1 -w 1500 1.1.1.1 >nul 2>&1
if errorlevel 1 (>> "%OUT%" echo Internet: NO ^(scenario viaggio^)) else (>> "%OUT%" echo Internet: SI)
>> "%OUT%" echo.

>> "%OUT%" echo === Processi in esecuzione adesso ===
for %%P in (steam.exe EpicGamesLauncher.exe EpicWebHelper.exe HITMAN3.exe PeacockPatcher.exe node.exe GamingServices.exe XboxPcApp.exe) do (
    tasklist /FI "IMAGENAME eq %%P" 2>nul | find /I "%%P" >nul
    if errorlevel 1 (>> "%OUT%" echo %%P : no) else (>> "%OUT%" echo %%P : IN ESECUZIONE)
)
>> "%OUT%" echo.

>> "%OUT%" echo === Cartella del gioco: %GAMEDIR% ===
if exist "%GAMEDIR%\HITMAN3.exe" (
    for %%F in (steam_api64.dll steam_appid.txt EOSSDK-Win64-Shipping.dll Epic.dll Windows.Gaming.dll MicrosoftGame.config Launcher.exe) do (
        if exist "%GAMEDIR%\%%F" (>> "%OUT%" echo %%F : PRESENTE) else (>> "%OUT%" echo %%F : -)
    )
    for %%F in ("%GAMEDIR%\HITMAN3.exe") do >> "%OUT%" echo HITMAN3.exe : %%~zF byte  %%~tF
    if exist "%GAMEDIR%\..\.egstore" (>> "%OUT%" echo ..\.egstore : PRESENTE ^(Epic^)) else (>> "%OUT%" echo ..\.egstore : -)
    if exist "%GAMEDIR%\..\steam_appid.txt" (>> "%OUT%" echo ..\steam_appid.txt : PRESENTE) else (>> "%OUT%" echo ..\steam_appid.txt : -)
) else (
    >> "%OUT%" echo HITMAN3.exe NON trovato in %GAMEDIR%
)
>> "%OUT%" echo.

>> "%OUT%" echo === Steam / Epic installati ===
reg query "HKCU\Software\Valve\Steam" /v SteamPath >> "%OUT%" 2>&1
reg query "HKCU\Software\Valve\Steam" /v Offline >> "%OUT%" 2>&1
if exist "C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests" (
    >> "%OUT%" echo Epic manifest presenti:
    findstr /I /M "HITMAN" "C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\*.item" >> "%OUT%" 2>&1
) else (>> "%OUT%" echo Epic Launcher: nessun manifest)
>> "%OUT%" echo.

>> "%OUT%" echo === Profili Peacock ===
for %%D in ("%PK%" "%PK2%") do (
    if exist "%%~D\userdata\users" (
        >> "%OUT%" echo --- %%~D\userdata\users ---
        dir /-C /O-S "%%~D\userdata\users" | findstr /V /I "Volume Directory byte" >> "%OUT%"
    ) else (>> "%OUT%" echo --- %%~D : nessuna cartella userdata\users ---)
)
>> "%OUT%" echo.
if exist "%PK%\options.ini" (
    >> "%OUT%" echo --- %PK%\options.ini ---
    findstr /V /R "^#" "%PK%\options.ini" | findstr /V /R "^$" >> "%OUT%"
)
>> "%OUT%" echo.

>> "%OUT%" echo === Ultimo log del server Peacock in %PK%\logs ===
set "LOG="
if exist "%PK%\logs" (
    for /f "delims=" %%L in ('dir /B /O-D "%PK%\logs\peacock-*.json" 2^>nul') do (
        if not defined LOG set "LOG=%%L"
    )
)
if defined LOG (
    for %%F in ("%PK%\logs\!LOG!") do >> "%OUT%" echo !LOG!  %%~zF byte  %%~tF
    >> "%OUT%" echo --- righe con oauth / entitlement / errori ---
    findstr /I "oauth authentication entitlement error ECONN ENOTFOUND steam epic EpicId SteamId Unauthor" "%PK%\logs\!LOG!" >> "%OUT%" 2>&1
    >> "%OUT%" echo --- ultime righe ---
    for /f "tokens=1* delims=:" %%A in ('find /C /V "" "%PK%\logs\!LOG!"') do set "N=%%B"
    set /a SKIP=!N!-25
    if !SKIP! lss 0 set SKIP=0
    more +!SKIP! "%PK%\logs\!LOG!" >> "%OUT%"
) else (
    >> "%OUT%" echo NESSUN log: il server in %PK% non e' mai partito, oppure non ha ricevuto nulla.
)
>> "%OUT%" echo.

>> "%OUT%" echo === Log Peacock in %PK2%\logs (per confronto) ===
if exist "%PK2%\logs" (dir /B /O-D "%PK2%\logs" >> "%OUT%" 2>&1) else (>> "%OUT%" echo nessuno)
>> "%OUT%" echo.
>> "%OUT%" echo === FINE ===

echo.
echo  Scritto: %OUT%
echo  Si apre il Blocco note: Ctrl+A, Ctrl+C e incolla in chat.
start "" notepad.exe "%OUT%"
endlocal
