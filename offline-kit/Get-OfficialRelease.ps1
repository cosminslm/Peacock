#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$module = Join-Path $PSScriptRoot 'lib\PeacockOffline.psm1'
Import-Module $module -Force

Write-Host ''
Write-Host '  SCARICA RELEASE UFFICIALE PEACOCK (packaged, giocabile)' -ForegroundColor Magenta
Write-Host '  NON scarica il codice sorgente. Prende Peacock-vX.Y.Z.zip da GitHub Releases.' -ForegroundColor DarkGray
Write-Host ''

$cfg = Read-OfflineConfig
$destRoot = $cfg.preferredInstallDir
if (-not $destRoot -or $destRoot.Trim() -eq '') {
    $destRoot = Join-Path $env:USERPROFILE 'Documents\Peacock'
}

$bad = @(
    ${env:ProgramFiles},
    ${env:ProgramFiles(x86)},
    'C:\Games\HITMAN - World of Assassination'
)
foreach ($b in $bad) {
    if ($b -and $destRoot.StartsWith($b, [StringComparison]::OrdinalIgnoreCase)) {
        Write-ErrLine "Non installare Peacock in '$destRoot' (Program Files / cartella del gioco)."
        $destRoot = Join-Path $env:USERPROFILE 'Documents\Peacock'
        Write-Info "Uso invece: $destRoot"
    }
}

Write-Info "Cartella di destinazione: $destRoot"
$rel = Get-LatestPeacockRelease
Write-Info "Release: $($rel.Tag)  $($rel.File)"
Write-Info "URL: $($rel.Url)"

$tmp = Join-Path $env:TEMP $rel.File
Write-Step 'Download'
Write-Host "Scarico in $tmp ..."
Invoke-WebRequest -Uri $rel.Url -OutFile $tmp -UseBasicParsing
Write-Ok "Scaricato $((Get-Item $tmp).Length) byte"

Write-Step 'Estrazione'
if (-not (Test-Path -LiteralPath $destRoot)) {
    New-Item -ItemType Directory -Path $destRoot | Out-Null
}

$extractTo = Join-Path $env:TEMP ("peacock-extract-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $extractTo | Out-Null
Expand-Archive -LiteralPath $tmp -DestinationPath $extractTo -Force

$inner = Get-ChildItem -LiteralPath $extractTo -Directory | Select-Object -First 1
if (-not $inner) { throw 'ZIP senza cartella interna. Release inattesa.' }

# Copia il contenuto della cartella Peacock-vX.Y.Z dentro destRoot
Get-ChildItem -LiteralPath $inner.FullName | ForEach-Object {
    $target = Join-Path $destRoot $_.Name
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force
    }
    Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force
}

Remove-Item -LiteralPath $extractTo -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue

Unblock-PeacockTree $destRoot

if (-not (Test-IsPackagedPeacock $destRoot)) {
    Write-ErrLine "Estrazione incompleta. Attesi chunk0.js, nodedist\node.exe, PeacockPatcher.exe in $destRoot"
    exit 1
}

$ver = Get-PeacockVersionFromDir $destRoot
Write-Ok "Peacock packaged v$ver pronto in $destRoot"

$cfg.peacockDir = $destRoot
if (-not $cfg.preferredInstallDir) { $cfg.preferredInstallDir = $destRoot }
Save-OfflineConfig $cfg

Write-Host ''
Write-Host 'Prossimi passi:' -ForegroundColor Cyan
Write-Host '  1. EsclusioneDefender.cmd  (come amministratore)'
Write-Host '  2. Gioca.cmd'
Write-Host ''
if ($Host.Name -eq 'ConsoleHost') {
    Write-Host 'Premi un tasto per chiudere...' -ForegroundColor DarkGray
    [void][System.Console]::ReadKey($true)
}
