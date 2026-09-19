#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# Rimette il JSON dal .bak piu grande (progressi veri) e punta
# il launcher a Documents\Peacock. Chiudi gioco e server prima.

$module = Join-Path $PSScriptRoot 'lib\Locale.psm1'
Import-Module $module -Force -DisableNameChecking

Write-Host ''
Write-Host '  RIPRISTINO profilo (il .bak con XP, non i JSON vuoti)' -ForegroundColor Magenta
Write-Host ''

$docs = Join-Path $env:USERPROFILE 'Documents\Peacock'
$games = 'C:\Games\HITMAN - World of Assassination\Peacock'
$roots = @($docs, $games)

$candidates = New-Object System.Collections.Generic.List[object]
foreach ($root in $roots) {
    $users = Join-Path $root 'userdata\users'
    if (-not (Test-Path -LiteralPath $users)) { continue }
    Get-ChildItem -LiteralPath $users -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'dbb3dd5d*' -or ($_.Extension -in '.json', '.bak' -and $_.BaseName -ne 'lop') } |
        ForEach-Object {
            [void]$candidates.Add([pscustomobject]@{
                    Path   = $_.FullName
                    Length = $_.Length
                    Time   = $_.LastWriteTime
                    Root   = $root
                })
        }
}

Write-Step 'File trovati'
if ($candidates.Count -eq 0) {
    Write-ErrLine 'Nessun profilo dbb3dd5d trovato.'
    exit 1
}
$candidates | Sort-Object Length -Descending | ForEach-Object {
    Write-Host ("  {0,8} byte  {1:yyyy-MM-dd HH:mm}  {2}" -f $_.Length, $_.Time, $_.Path)
}

$best = $candidates | Sort-Object Length -Descending | Select-Object -First 1
Write-Ok ("Sorgente (il piu grande): {0} byte" -f $best.Length)
if ($best.Length -lt 20000) {
    Write-WarnLine 'Anche il piu grande e piccolo. Controlla di avere il .bak da 38695 byte.'
}

$destRoot = $docs
if (-not (Test-IsPackagedPeacock $destRoot)) {
    $destRoot = $best.Root
}
if (-not (Test-IsPackagedPeacock $destRoot)) {
    Write-ErrLine "Peacock packaged non trovato in $destRoot"
    exit 1
}

$usersDest = Join-Path $destRoot 'userdata\users'
$guid = 'dbb3dd5d-5df5-469c-9e35-271fa86815d8'
# keep original filename if the bak is for that uuid
$leaf = [IO.Path]::GetFileName($best.Path) -replace '\.bak$', '' -replace '\.pre-sfilata$', ''
if ($leaf -notlike '*.json') { $leaf = $guid + '.json' }
$destJson = Join-Path $usersDest $leaf

Write-Step "Ripristino in $destJson"
if (Test-Path -LiteralPath $destJson) {
    $safety = $destJson + '.prima-ripristino.bak'
    Copy-Item -LiteralPath $destJson -Destination $safety -Force
    Write-Info "Copia di sicurezza del JSON vuoto: $safety"
}

$bytes = [IO.File]::ReadAllBytes($best.Path)
[IO.File]::WriteAllBytes($destJson, $bytes)
Write-Ok ("Scritto {0} byte" -f $bytes.Length)

Restore-OwnedWoaEntitlements $destRoot
Set-OfflineFriendlyOptions $destRoot

$cfgPath = Join-Path $PSScriptRoot 'config.json'
$cfg = @{
    peacockDir = $destRoot
    gameExe    = 'C:\Games\HITMAN - World of Assassination\Retail\HITMAN3.exe'
    serverUrl  = '127.0.0.1'
}
$cfg | ConvertTo-Json | Set-Content -LiteralPath $cfgPath -Encoding UTF8
Write-Ok "config.json -> $destRoot"

Write-Host ''
Write-Host 'Da ora il server DEVE essere Documents\Peacock, non C:\Games\...\Peacock.' -ForegroundColor Yellow
Write-Host (Get-UserdataSummary $destRoot) -ForegroundColor Yellow
Write-Host 'Poi: Gioca.cmd admin. Hub Planning Start.' -ForegroundColor Cyan
Write-Host ''
if ($Host.Name -eq 'ConsoleHost') {
    Write-Host 'Premi un tasto...' -ForegroundColor DarkGray
    [void][System.Console]::ReadKey($true)
}
