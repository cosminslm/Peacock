#Requires -Version 5.1
param(
    [switch]$Force
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$module = Join-Path $PSScriptRoot 'lib\PeacockOffline.psm1'
Import-Module $module -Force

Write-Host ''
Write-Host '  RELEASE PEACOCK (download solo se manca, userdata mai cancellato)' -ForegroundColor Magenta
Write-Host ''

$existing = Resolve-PackagedPeacockDir -AllowMissing
if ($existing -and -not $Force) {
    Write-Ok "Trovata installazione packaged. NON scarico di nuovo."
    Write-Info $existing
    Write-Info (Get-UserdataSummary $existing)
    Write-Host 'Per forzare un aggiornamento: Get-OfficialRelease.ps1 -Force' -ForegroundColor DarkGray
    $cfg = Read-OfflineConfig
    $cfg.peacockDir = $existing
    Save-OfflineConfig $cfg
    if ($Host.Name -eq 'ConsoleHost') {
        Write-Host 'Premi un tasto per chiudere...' -ForegroundColor DarkGray
        [void][System.Console]::ReadKey($true)
    }
    exit 0
}

$cfg = Read-OfflineConfig
$destRoot = if ($existing) { $existing } elseif ($cfg.preferredInstallDir) { $cfg.preferredInstallDir } else { Join-Path $env:USERPROFILE 'Documents\Peacock' }

$bad = @(
    ${env:ProgramFiles},
    ${env:ProgramFiles(x86)}
)
foreach ($b in $bad) {
    if ($b -and $destRoot.StartsWith($b, [StringComparison]::OrdinalIgnoreCase)) {
        $destRoot = Join-Path $env:USERPROFILE 'Documents\Peacock'
        Write-Info "Uso invece: $destRoot"
    }
}

[void](Install-OfficialPeacockRelease -DestRoot $destRoot -Force:$Force)
$cfg.peacockDir = $destRoot
if (-not $cfg.preferredInstallDir) { $cfg.preferredInstallDir = $destRoot }
Save-OfflineConfig $cfg

Write-Host ''
Write-Host 'Basta. Da ora usa solo Gioca.cmd come amministratore.' -ForegroundColor Cyan
if ($Host.Name -eq 'ConsoleHost') {
    Write-Host 'Premi un tasto per chiudere...' -ForegroundColor DarkGray
    [void][System.Console]::ReadKey($true)
}
