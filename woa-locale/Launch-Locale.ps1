#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# Avvia SOLO Peacock gia presente sul disco. Zero download.

$module = Join-Path $PSScriptRoot 'lib\Locale.psm1'
Import-Module $module -Force -DisableNameChecking

if (-not (Request-Administrator -ScriptPath $PSCommandPath)) {
    exit 0
}

function Stop-OwnedProcess {
    param([System.Diagnostics.Process]$Proc)
    if (-not $Proc) { return }
    try {
        if (-not $Proc.HasExited) {
            $Proc.CloseMainWindow() | Out-Null
            if (-not $Proc.WaitForExit(4000)) { $Proc.Kill() }
        }
    } catch {}
}

Write-Host ''
Write-Host '  HITMAN WOA  -  PEACOCK LOCALE (zero download)' -ForegroundColor Magenta
Write-Host ''

$cfg = Read-LocaleConfig
$peacockDir = [string]$cfg.peacockDir
$game = [string]$cfg.gameExe
$serverUrl = [string]$cfg.serverUrl
if (-not $serverUrl) { $serverUrl = '127.0.0.1' }

Write-Step 'Peacock sul disco (nessun GitHub)'
if (-not (Test-IsPackagedPeacock $peacockDir)) {
    Write-ErrLine "Peacock locale non trovato (servono chunk0.js + nodedist + PeacockPatcher.exe):"
    Write-Host "  $peacockDir"
    Write-Host 'Correggi peacockDir in config.json. Questo launcher NON scarica nulla.'
    if ($Host.Name -eq 'ConsoleHost') { [void][System.Console]::ReadKey($true) }
    exit 1
}
Write-Ok $peacockDir
Write-Info (Get-UserdataSummary $peacockDir)

$patcher = Join-Path $peacockDir 'PeacockPatcher.exe'
$node = Join-Path $peacockDir 'nodedist\node.exe'
$chunk = Join-Path $peacockDir 'chunk0.js'

Set-OfflineFriendlyOptions $peacockDir
Write-Step 'Pacchetti sul profilo locale'
Restore-OwnedWoaEntitlements $peacockDir

Write-Host ''
Write-Host 'SALVATAGGIO SOLO QUI:' -ForegroundColor Yellow
Write-Host "  $peacockDir\userdata\users"
Write-Host (Get-UserdataSummary $peacockDir) -ForegroundColor Yellow

Write-Step 'Porta 80'
$owner = Get-Port80Owner
$serverProc = $null
$alreadyUp = $false
if (Test-HttpLocalhost -HostName $serverUrl) {
    if (Test-Port80IsThisPeacock $peacockDir) {
        Write-Ok 'Server gia avviato da QUESTA cartella.'
        $alreadyUp = $true
    } else {
        Write-ErrLine "Porta 80 occupata da un altro processo ($owner). Chiudi le altre finestre Peacock Server."
        if ($Host.Name -eq 'ConsoleHost') { [void][System.Console]::ReadKey($true) }
        exit 1
    }
} elseif ($owner) {
    Write-ErrLine "Porta 80 occupata da $owner."
    if ($Host.Name -eq 'ConsoleHost') { [void][System.Console]::ReadKey($true) }
    exit 1
}

if (-not $alreadyUp) {
    Write-Step 'Avvio server (node locale)'
    $cmdArgs = '/k title Peacock Server && nodedist\node.exe chunk0.js'
    $serverProc = Start-Process -FilePath 'cmd.exe' -ArgumentList $cmdArgs -WorkingDirectory $peacockDir -PassThru
    Write-Info "PID $($serverProc.Id)"
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 1
        if ($serverProc.HasExited) {
            Write-ErrLine "Il server e uscito subito (codice $($serverProc.ExitCode))."
            exit 1
        }
        if (Test-HttpLocalhost -HostName $serverUrl) { $ready = $true; break }
    }
    if ($ready) { Write-Ok 'Server in ascolto.' } else { Write-WarnLine 'Timeout HTTP. Proseguo.' }
}

Write-Step 'PeacockPatcher'
$patcherProc = Start-Process -FilePath $patcher -WorkingDirectory $peacockDir -PassThru
Write-Ok "Patcher PID $($patcherProc.Id) -> $serverUrl"

$gameProc = $null
if ($game -and (Test-Path -LiteralPath $game)) {
    Write-Step 'HITMAN WOA'
    Write-Info $game
    $gameProc = Start-Process -FilePath $game -WorkingDirectory (Split-Path -Parent $game) -PassThru
    Write-Ok "Gioco PID $($gameProc.Id)"
    Write-Host 'Patcher: Successfully patched + Injected server 127.0.0.1' -ForegroundColor Cyan
    Write-Host 'Hub -> Planning -> Start. Niente Continua/Carica.' -ForegroundColor Cyan
} else {
    Write-WarnLine "HITMAN3.exe non trovato: $game  (modifica gameExe in config.json)"
}

Write-Host ''
Write-Host 'Lascia aperte le finestre. Fine partita: esci, 10 secondi, Q.' -ForegroundColor Green
Write-Host ''

while ($true) {
    if ([Console]::KeyAvailable) {
        $k = [Console]::ReadKey($true)
        if ($k.Key -eq 'Q') { break }
    }
    Start-Sleep -Milliseconds 400
    if ($patcherProc.HasExited -and $serverProc -and $serverProc.HasExited) { break }
}

Write-Step 'Flush profilo (8s)'
Start-Sleep -Seconds 8
Stop-OwnedProcess $patcherProc
if ($serverProc) { Stop-OwnedProcess $serverProc }
Write-Host (Get-UserdataSummary $peacockDir) -ForegroundColor Yellow
Write-Ok 'Chiuso. Prossima volta: solo Gioca.cmd (admin). Nessun download.'
