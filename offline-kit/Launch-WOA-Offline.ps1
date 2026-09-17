#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$module = Join-Path $PSScriptRoot 'lib\PeacockOffline.psm1'
Import-Module $module -Force

if (-not (Request-Administrator -ScriptPath $PSCommandPath)) {
    exit 0
}

function Stop-OwnedProcess {
    param([System.Diagnostics.Process]$Proc)
    if (-not $Proc) { return }
    try {
        if (-not $Proc.HasExited) {
            $Proc.CloseMainWindow() | Out-Null
            if (-not $Proc.WaitForExit(3000)) {
                $Proc.Kill()
            }
        }
    } catch {}
}

Write-Host ''
Write-Host '  HITMAN WOA  -  AVVIO OFFLINE CON PEACOCK' -ForegroundColor Magenta
Write-Host ''

$cfg = Read-OfflineConfig

Write-Step 'Verifica installazione packaged'
try {
    $peacockDir = Resolve-PackagedPeacockDir
} catch {
    Write-ErrLine $_.Exception.Message
    Write-Host 'Esegui ScaricaRelease.cmd e riprova.'
    if ($Host.Name -eq 'ConsoleHost') { [void][System.Console]::ReadKey($true) }
    exit 1
}
$ver = Get-PeacockVersionFromDir $peacockDir
Write-Ok "Peacock packaged v$ver"
Write-Info $peacockDir

if (Test-IsSourceCheckout $peacockDir) {
    Write-ErrLine 'Questa cartella e il SORGENTE, non una release. Stop.'
    exit 1
}

Unblock-PeacockTree $peacockDir

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
if ($cfg.launchGame -and -not $game) {
    Write-WarnLine 'HITMAN3.exe non trovato: avvio solo server + patcher. Imposta gameExe in config.json.'
}

if ($cfg.applyOfflineOptions) {
    Set-OfflineFriendlyOptions $peacockDir
}

Write-Step 'Porta 80'
$owner = Get-Port80Owner
$serverProc = $null
$alreadyUp = $false
if (Test-HttpLocalhost -HostName $cfg.serverUrl) {
    Write-Info 'Un server risponde gia su http://127.0.0.1/ - riutilizzo quello.'
    $alreadyUp = $true
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
    if ($ready) { Write-Ok 'Server in ascolto.' } else { Write-WarnLine 'Timeout attesa HTTP. Proseguo comunque: il patcher puo agganciarsi dopo.' }
}

Write-Step 'Avvio PeacockPatcher'
$patcherProc = Start-Process -FilePath $patcher -WorkingDirectory $peacockDir -PassThru
Write-Ok "Patcher PID $($patcherProc.Id)  (attende HITMAN3.exe e reindirizza a $($cfg.serverUrl))"

$gameProc = $null
if ($cfg.launchGame -and $game) {
    Write-Step 'Avvio HITMAN WOA'
    Write-Info $game
    $gameProc = Start-Process -FilePath $game -WorkingDirectory (Split-Path -Parent $game) -PassThru
    Write-Ok "Gioco PID $($gameProc.Id)"
    Write-Host ''
    Write-Host 'Nella finestra del patcher devi vedere "Successfully patched processid ..."' -ForegroundColor Cyan
    Write-Host 'Se non compare: tieni il patcher aperto, torna al menu del gioco, aspetta 2-3 secondi.' -ForegroundColor Cyan
}

Write-Host ''
Write-Host 'Peacock e in esecuzione. Chiudi questa finestra SOLO quando hai finito di giocare.' -ForegroundColor Green
Write-Host 'Tasto Q + Invio = chiudi patcher (e server se avviato da qui).' -ForegroundColor DarkGray
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

Write-Step 'Chiusura'
Stop-OwnedProcess $patcherProc
if ($serverProc) { Stop-OwnedProcess $serverProc }
Write-Ok 'Fatto.'
