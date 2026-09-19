#Requires -Version 5.1
# Raccoglie informazioni (nessuna modifica, nessun download) e apre un .txt da incollare in chat.
$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$module = Join-Path $PSScriptRoot 'lib\Locale.psm1'
Import-Module $module -Force -DisableNameChecking

$cfg = Read-LocaleConfig
$peacockDir = [string]$cfg.peacockDir
$game = [string]$cfg.gameExe
$out = Join-Path ([Environment]::GetFolderPath('Desktop')) 'peacock-diagnosi.txt'
$lines = New-Object System.Collections.Generic.List[string]
function L([string]$s) { $lines.Add($s) }

L "=== DIAGNOSI PEACOCK  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ==="
L "peacockDir : $peacockDir"
L "gameExe    : $game"
L ""

L "=== Rete ==="
L ("Internet 1.1.1.1:443 raggiungibile: " + (Test-InternetReachable))
L ""

L "=== Processi ora ==="
foreach ($n in 'steam','EpicGamesLauncher','EpicWebHelper','HITMAN3','PeacockPatcher','node','GamingServices','XboxPcApp') {
    $p = Get-Process -Name $n -ErrorAction SilentlyContinue
    L ("{0,-18} {1}" -f $n, $(if ($p) { 'IN ESECUZIONE (PID ' + (($p | Select-Object -ExpandProperty Id) -join ',') + ')' } else { 'no' }))
}
L ""

L "=== Cartella del gioco (che piattaforma e') ==="
if ($game -and (Test-Path -LiteralPath $game)) {
    $gdir = Split-Path -Parent $game
    foreach ($f in 'steam_api64.dll','steam_appid.txt','EOSSDK-Win64-Shipping.dll','Epic.dll','Windows.Gaming.dll','MicrosoftGame.config','launcher.exe','Launcher.exe') {
        $fp = Join-Path $gdir $f
        L ("{0,-28} {1}" -f $f, $(if (Test-Path -LiteralPath $fp) { 'PRESENTE' } else { '-' }))
    }
    $fi = Get-Item -LiteralPath $game
    L ("HITMAN3.exe  {0} byte  {1}" -f $fi.Length, $fi.LastWriteTime)
    $parent = Split-Path -Parent $gdir
    foreach ($f in 'steam_appid.txt','.egstore','installscript.vdf') {
        $fp = Join-Path $parent $f
        L ("{0,-28} {1}" -f ("..\" + $f), $(if (Test-Path -LiteralPath $fp) { 'PRESENTE' } else { '-' }))
    }
} else { L "HITMAN3.exe NON trovato." }
L ""

L "=== Steam / Epic installati? ==="
$steamReg = Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue
L ("Steam path (registro): " + $(if ($steamReg) { $steamReg.SteamPath } else { 'nessuno' }))
$steamOffline = Get-ItemProperty 'HKCU:\Software\Valve\Steam' -Name 'Offline' -ErrorAction SilentlyContinue
L ("Steam Offline flag   : " + $(if ($steamOffline) { $steamOffline.Offline } else { 'n/d' }))
$epic = Get-ChildItem 'C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests' -Filter *.item -ErrorAction SilentlyContinue
L ("Epic manifest        : " + $(if ($epic) { $epic.Count.ToString() + ' giochi' } else { 'nessuno' }))
foreach ($m in $epic) {
    try {
        $j = Get-Content -LiteralPath $m.FullName -Raw | ConvertFrom-Json
        if ($j.DisplayName -match 'HITMAN') { L ("  Epic: {0} -> {1}" -f $j.DisplayName, $j.InstallLocation) }
    } catch {}
}
L ""

L "=== Profili Peacock ==="
foreach ($d in @($peacockDir, 'C:\Games\HITMAN - World of Assassination\Peacock', (Join-Path $env:USERPROFILE 'Documents\Peacock'))) {
    $u = Join-Path $d 'userdata\users'
    if (Test-Path -LiteralPath $u) {
        L "$u"
        Get-ChildItem -LiteralPath $u -File | ForEach-Object {
            $xp = ''
            if ($_.Extension -eq '.json') {
                try { $j = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json; $xp = " XP=$($j.Extensions.progression.PlayerProfileXP.Total)" } catch {}
            }
            L ("   {0,-55} {1,7} byte  {2}{3}" -f $_.Name, $_.Length, $_.LastWriteTime.ToString('dd/MM HH:mm'), $xp)
        }
    }
}
$opt = Join-Path $peacockDir 'options.ini'
if (Test-Path -LiteralPath $opt) { L ""; L "--- options.ini ---"; Get-Content -LiteralPath $opt | Where-Object { $_ -notmatch '^\s*(#|;|$)' } | ForEach-Object { L $_ } }
L ""

L "=== Ultimo log del server Peacock (cosa ha chiesto il gioco) ==="
$logDir = Join-Path $peacockDir 'logs'
$log = Get-ChildItem -LiteralPath $logDir -Filter 'peacock-*.json' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($log) {
    L "$($log.FullName)  ($($log.Length) byte, $($log.LastWriteTime))"
    $all = Get-Content -LiteralPath $log.FullName -ErrorAction SilentlyContinue
    $hits = $all | Where-Object { $_ -match 'oauth|authentication|entitlement|config/pc|profiles/page|error|Error|ECONN|EAI_AGAIN|ENOTFOUND|Steam|Epic|license' } | Select-Object -Last 40
    L "--- righe rilevanti (ultime 40) ---"
    foreach ($h in $hits) { L ($h -replace '"timestamp":"[^"]+",?', '') }
    L "--- ultime 25 righe assolute ---"
    foreach ($h in ($all | Select-Object -Last 25)) { L ($h -replace '"timestamp":"[^"]+",?', '') }
} else {
    L "NESSUN log in $logDir  -> il server di questa cartella non e' mai partito o non scrive log."
}
L ""

L "=== Log del gioco (se esiste) ==="
foreach ($gl in @((Join-Path $env:LOCALAPPDATA 'IO Interactive\HITMAN3\'), (Join-Path $env:USERPROFILE 'Documents\IO Interactive\HITMAN3\'))) {
    if (Test-Path -LiteralPath $gl) {
        Get-ChildItem -LiteralPath $gl -Recurse -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 6 | ForEach-Object {
            L ("   {0}  {1} byte  {2}" -f $_.FullName, $_.Length, $_.LastWriteTime.ToString('dd/MM HH:mm'))
        }
    }
}

$lines | Set-Content -LiteralPath $out -Encoding UTF8
Write-Host "Scritto: $out" -ForegroundColor Green
Start-Process notepad.exe $out
