#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$module = Join-Path $PSScriptRoot 'lib\PeacockOffline.psm1'
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
            if (-not $Proc.WaitForExit(4000)) {
                $Proc.Kill()
            }
        }
    } catch {}
}

Write-Host ''
Write-Host '  HITMAN WOA  -  UN SOLO AVVIO (admin)' -ForegroundColor Magenta
Write-Host '  Server + patcher + gioco. La release si scarica solo se manca.' -ForegroundColor DarkGray
Write-Host ''

$cfg = Read-OfflineConfig

Write-Step 'Peacock (niente re-download se gia c e)'
$peacockDir = Ensure-PackagedPeacock
$ver = Get-PeacockVersionFromDir $peacockDir
Write-Ok "v$ver"
Unblock-PeacockTree $peacockDir
Try-AddDefenderExclusion $peacockDir

$patcher = Join-Path $peacockDir 'PeacockPatcher.exe'
$node = Join-Path $peacockDir 'nodedist\node.exe'
$chunk = Join-Path $peacockDir 'chunk0.js'
foreach ($f in @($patcher, $node, $chunk)) {
    if (-not (Test-Path -LiteralPath $f)) {
        Write-ErrLine "Manca $f"
        exit 1
    }
}

$game = Find-HitmanExe $cfg.gameExe
$plat = Get-HitmanPlatform $game
if ($game) {
    Write-Step 'Piattaforma del gioco (non IOI)'
    Write-Info ("Rilevata: {0}  ->  {1}" -f $plat.Kind, $game)
    Write-Host $plat.Hint -ForegroundColor Cyan
    if (-not $plat.Supported) {
        Write-ErrLine 'Questa copia non e usabile con Peacock. Ferma qui.'
        if ($Host.Name -eq 'ConsoleHost') { [void][System.Console]::ReadKey($true) }
        exit 1
    }
    if (-not (Test-PlatformLauncherRunning $plat)) {
        if ($plat.Kind -eq 'epic') {
            Write-WarnLine 'Epic Games Launcher NON e in esecuzione. Aprilo, fai login (internet), poi rilancia Gioca.cmd. Steam non c entra.'
        } elseif ($plat.Kind -eq 'steam') {
            Write-WarnLine 'Steam NON e in esecuzione. Aprilo e loggati, poi rilancia Gioca.cmd.'
        }
    } else {
        if ($plat.Kind -eq 'epic') { Write-Ok 'Epic Games Launcher in esecuzione.' }
        elseif ($plat.Kind -eq 'steam') { Write-Ok 'Steam in esecuzione.' }
    }
}
if ($cfg.launchGame -and -not $game) {
    Write-WarnLine 'HITMAN3.exe non trovato: avvio solo server + patcher. Imposta gameExe in config.json.'
}

if ($cfg.applyOfflineOptions) {
    Set-OfflineFriendlyOptions $peacockDir
}

Write-Step 'Pacchetti WOA (entitlement, NON sblocca armi/mastery)'
Restore-OwnedWoaEntitlements $peacockDir

Write-AllPeacockCopies

Write-Host ''
Write-Host 'SALVATAGGIO SOLO QUI (se giochi su un altro Peacock, i progressi spariscono):' -ForegroundColor Yellow
Write-Host "  $peacockDir\userdata\users"
Write-Host (Get-UserdataSummary $peacockDir) -ForegroundColor Yellow
Write-Host 'Usa SEMPRE questa stessa cartella. Non aprire ScaricaRelease.cmd a ogni partita.' -ForegroundColor Yellow

Write-Step 'Porta 80'
$owner = Get-Port80Owner
$serverProc = $null
$alreadyUp = $false
if (Test-HttpLocalhost -HostName $cfg.serverUrl) {
    if (Test-Port80IsThisPeacock $peacockDir) {
        Write-Ok 'Server gia in ascolto DA QUESTA cartella Peacock. Lo riuso.'
        $alreadyUp = $true
    } else {
        Write-ErrLine "Porta 80 occupata da un ALTRO server ($owner)."
        Write-Host "I salvataggi andrebbero in un'altra cartella. Chiudi OGNI finestra 'Peacock Server' / node, poi rilancia Gioca.cmd."
        if ($Host.Name -eq 'ConsoleHost') { [void][System.Console]::ReadKey($true) }
        exit 1
    }
} elseif ($owner) {
    Write-ErrLine "Porta 80 occupata da $owner e non risponde come Peacock."
    Write-Host 'Chiudi IIS / altro web server, poi rilancia.'
    if ($Host.Name -eq 'ConsoleHost') { [void][System.Console]::ReadKey($true) }
    exit 1
}

if (-not $alreadyUp) {
    Write-Step 'Avvio server Peacock'
    $cmdArgs = '/k title Peacock Server && nodedist\node.exe chunk0.js'
    $serverProc = Start-Process -FilePath 'cmd.exe' -ArgumentList $cmdArgs -WorkingDirectory $peacockDir -PassThru
    Write-Info "Finestra server avviata (PID $($serverProc.Id))"
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 1
        if ($serverProc.HasExited) {
            Write-ErrLine "Il server e uscito subito (codice $($serverProc.ExitCode)). Controlla la finestra / logs."
            exit 1
        }
        if (Test-HttpLocalhost -HostName $cfg.serverUrl) {
            $ready = $true
            break
        }
    }
    if ($ready) { Write-Ok 'Server in ascolto.' } else { Write-WarnLine 'Timeout attesa HTTP. Proseguo comunque.' }
}

Write-Step 'Avvio PeacockPatcher'
$patcherProc = Start-Process -FilePath $patcher -WorkingDirectory $peacockDir -PassThru
Write-Ok "Patcher PID $($patcherProc.Id)  ->  $($cfg.serverUrl)"

$gameProc = $null
if ($cfg.launchGame -and $game) {
    Write-Step 'Avvio HITMAN WOA'
    Write-Info $game
    $gameProc = Start-Process -FilePath $game -WorkingDirectory (Split-Path -Parent $game) -PassThru
    Write-Ok "Gioco PID $($gameProc.Id)"
    Write-Host 'Patcher: aspetta "Successfully patched processid" e "Injected server: 127.0.0.1"' -ForegroundColor Cyan
    Write-Host 'Missioni: parti dal Hub (Planning -> Start), NON da Carica/Continua di save IOI.' -ForegroundColor Cyan
}

Write-Host ''
Write-Host 'Lascia aperte queste finestre. Quando hai finito: esci dal gioco, aspetta 10 secondi, poi Q.' -ForegroundColor Green
Write-Host ''

if ($gameProc -and $cfg.stopOnGameExit) {
    Write-Info 'Attendo la chiusura del gioco...'
    $gameProc.WaitForExit()
} else {
    while ($true) {
        if ([Console]::KeyAvailable) {
            $k = [Console]::ReadKey($true)
            if ($k.Key -eq 'Q') { break }
        }
        Start-Sleep -Milliseconds 400
        if ($patcherProc.HasExited -and $serverProc -and $serverProc.HasExited) { break }
    }
}

Write-Step 'Salvataggio profilo (attendo 8s, Peacock scrive ogni 3s)'
Start-Sleep -Seconds 8
Stop-OwnedProcess $patcherProc
if ($serverProc) { Stop-OwnedProcess $serverProc }
Write-Host (Get-UserdataSummary $peacockDir) -ForegroundColor Yellow
Write-Ok 'Chiuso. Controlla che data/ora del JSON siano di ADESSO. Prossima volta: solo Gioca.cmd (admin).'
