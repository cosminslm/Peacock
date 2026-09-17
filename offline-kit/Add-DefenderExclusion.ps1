#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$module = Join-Path $PSScriptRoot 'lib\PeacockOffline.psm1'
Import-Module $module -Force

if (-not (Request-Administrator -ScriptPath $PSCommandPath)) {
    exit 0
}

Write-Host ''
Write-Host '  ESCLUSIONE WINDOWS DEFENDER PER PEACOCK' -ForegroundColor Magenta
Write-Host '  PeacockPatcher.exe modifica la memoria di HITMAN3.exe per puntare a localhost.' -ForegroundColor DarkGray
Write-Host '  Defender lo classifica spesso come falso positivo e lo mette in quarantena.' -ForegroundColor DarkGray
Write-Host ''

$cfg = Read-OfflineConfig
$dirs = New-Object System.Collections.Generic.List[string]
if ($cfg.peacockDir) { [void]$dirs.Add($cfg.peacockDir) }
$packaged = Resolve-PackagedPeacockDir -AllowMissing
if ($packaged) { [void]$dirs.Add($packaged) }
[void]$dirs.Add((Get-KitRoot))

$unique = $dirs | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -Unique
if ($unique.Count -eq 0) {
    Write-ErrLine 'Nessuna cartella Peacock da escludere. Esegui prima ScaricaRelease.cmd.'
    exit 1
}

try {
    $null = Get-Command Add-MpPreference -ErrorAction Stop
} catch {
    Write-ErrLine 'Add-MpPreference non disponibile. Aggiungi a mano: Sicurezza Windows -> Protezione da virus -> Esclusioni.'
    foreach ($d in $unique) { Write-Host "  $d" }
    exit 1
}

foreach ($d in $unique) {
    Write-Info "Aggiungo esclusione: $d"
    Add-MpPreference -ExclusionPath $d
    Unblock-PeacockTree $d
    $patcher = Join-Path $d 'PeacockPatcher.exe'
    if (Test-Path -LiteralPath $patcher) {
        try { Add-MpPreference -ExclusionProcess 'PeacockPatcher.exe' } catch {}
        try { Add-MpPreference -ExclusionPath $patcher } catch {}
    }
    Write-Ok "Esclusione attiva per $d"
}

Write-Host ''
Write-Host 'Se il patcher era in quarantena:' -ForegroundColor Yellow
Write-Host '  Sicurezza Windows -> Protezione da virus e minacce -> Cronologia -> Ripristina PeacockPatcher.exe'
Write-Host ''
if ($Host.Name -eq 'ConsoleHost') {
    Write-Host 'Premi un tasto per chiudere...' -ForegroundColor DarkGray
    [void][System.Console]::ReadKey($true)
}
