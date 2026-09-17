#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Ok([string]$Message) { Write-Host "[OK]    $Message" -ForegroundColor Green }
function Write-Info([string]$Message) { Write-Host "[INFO]  $Message" -ForegroundColor Cyan }
function Write-WarnLine([string]$Message) { Write-Host "[WARN]  $Message" -ForegroundColor Yellow }
function Write-ErrLine([string]$Message) { Write-Host "[ERRORE] $Message" -ForegroundColor Red }
function Write-Step([string]$Message) { Write-Host "`n=== $Message ===" -ForegroundColor Magenta }

function Get-KitRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }
    return (Get-Location).Path
}

function Get-ConfigPath {
    Join-Path (Get-KitRoot) 'config.json'
}

function Get-DefaultConfig {
    [pscustomobject]@{
        peacockDir           = ''
        gameExe              = 'C:\Games\HITMAN - World of Assassination\Retail\HITMAN3.exe'
        serverUrl            = '127.0.0.1'
        launchGame           = $true
        stopOnGameExit       = $false
        applyOfflineOptions  = $true
        preferredInstallDir  = ''
    }
}

function Read-OfflineConfig {
    $path = Get-ConfigPath
    $cfg = Get-DefaultConfig
    if (Test-Path -LiteralPath $path) {
        $raw = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
        foreach ($p in $cfg.PSObject.Properties.Name) {
            if ($null -ne $raw.$p -and "$($raw.$p)" -ne '') {
                $cfg.$p = $raw.$p
            }
        }
    }
    return $cfg
}

function Save-OfflineConfig($Config) {
    $path = Get-ConfigPath
    $Config | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $path -Encoding UTF8
    Write-Ok "Config salvata in $path"
}

function Test-IsAdministrator {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $p = New-Object Security.Principal.WindowsPrincipal($id)
    return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Request-Administrator {
    param(
        [string]$ScriptPath,
        [string[]]$ArgumentList = @()
    )
    if (Test-IsAdministrator) { return $true }

    Write-WarnLine 'Servono privilegi amministratore (porta 80 + patcher). Rilancio elevato...'
    $argString = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$ScriptPath`"") + $ArgumentList
    try {
        Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $argString | Out-Null
        return $false
    } catch {
        Write-ErrLine "Elevazione rifiutata: $($_.Exception.Message)"
        return $false
    }
}

function Test-IsPackagedPeacock {
    param([string]$Dir)
    if (-not (Test-Path -LiteralPath $Dir)) { return $false }
    $chunk = Join-Path $Dir 'chunk0.js'
    $patcher = Join-Path $Dir 'PeacockPatcher.exe'
    $node = Join-Path $Dir 'nodedist\node.exe'
    return (Test-Path -LiteralPath $chunk) -and (Test-Path -LiteralPath $patcher) -and (Test-Path -LiteralPath $node)
}

function Test-IsSourceCheckout {
    param([string]$Dir)
    if (-not (Test-Path -LiteralPath $Dir)) { return $false }
    $pkg = Join-Path $Dir 'package.json'
    $components = Join-Path $Dir 'components'
    $hasChunk = Test-Path -LiteralPath (Join-Path $Dir 'chunk0.js')
    $hasNodeDist = Test-Path -LiteralPath (Join-Path $Dir 'nodedist\node.exe')
    return (Test-Path -LiteralPath $pkg) -and (Test-Path -LiteralPath $components) -and (-not $hasChunk) -and (-not $hasNodeDist)
}

function Get-PeacockVersionFromDir {
    param([string]$Dir)
    $pkg = Join-Path $Dir 'package.json'
    if (Test-Path -LiteralPath $pkg) {
        try {
            $j = Get-Content -LiteralPath $pkg -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($j.version -and $j.version -match '^[678]\.') { return [string]$j.version }
            if ($j.version) { return [string]$j.version }
        } catch {}
    }
    $chunk = Join-Path $Dir 'chunk0.js'
    if (Test-Path -LiteralPath $chunk) {
        $text = $null
        try {
            $fs = [System.IO.File]::Open($chunk, 'Open', 'Read', 'ReadWrite')
            try {
                $len = [Math]::Min(512kb, $fs.Length)
                $buf = New-Object byte[] $len
                [void]$fs.Read($buf, 0, $len)
                $text = [System.Text.Encoding]::UTF8.GetString($buf)
            } finally { $fs.Close() }
        } catch {
            $text = Get-Content -LiteralPath $chunk -TotalCount 5 -Raw -ErrorAction SilentlyContinue
        }
        if ($text) {
            foreach ($re in @(
                    'This is Peacock v(\d+\.\d+\.\d+)',
                    'Peacock v(\d+\.\d+\.\d+)',
                    'HUMAN_VERSION["''`:\s=]+(\d+\.\d+\.\d+)'
                )) {
                if ($text -match $re) { return $Matches[1] }
            }
            $found = [regex]::Matches($text, '(?<!\d)([678]\.\d+\.\d+)(?!\d)')
            if ($found.Count -gt 0) { return $found[0].Groups[1].Value }
        }
    }
    $patcher = Join-Path $Dir 'PeacockPatcher.exe'
    if (Test-Path -LiteralPath $patcher) {
        $p = Get-Item -LiteralPath $patcher
        if ($p.Length -ge 280000 -and $p.Length -le 320000 -and $p.LastWriteTime.Year -eq 2023) {
            return '6.5.x (2023, da data patcher)'
        }
    }
    return 'sconosciuta'
}

function Get-CandidatePeacockDirs {
    $cfg = Read-OfflineConfig
    $list = New-Object System.Collections.Generic.List[string]
    $add = {
        param($p)
        if ($p -and $p.Trim() -ne '' -and -not $list.Contains($p)) { [void]$list.Add($p) }
    }
    & $add $cfg.peacockDir
    & $add $cfg.preferredInstallDir
    & $add (Join-Path $env:USERPROFILE 'Documents\Peacock')
    & $add (Join-Path $env:USERPROFILE 'Desktop\Peacock')
    & $add 'C:\Games\HITMAN - World of Assassination\Peacock'
    & $add (Join-Path $env:USERPROFILE 'Downloads\Peacock-master\Peacock-master')
    & $add (Join-Path $env:USERPROFILE 'Downloads\Peacock-master')
    Get-ChildItem -Path (Join-Path $env:USERPROFILE 'Downloads') -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'Peacock-v*' -or $_.Name -eq 'Peacock' } |
        ForEach-Object { & $add $_.FullName }
    return $list
}

function Resolve-PackagedPeacockDir {
    param([switch]$AllowMissing)
    $cfg = Read-OfflineConfig
    $candidates = Get-CandidatePeacockDirs
    foreach ($dir in $candidates) {
        if (Test-IsPackagedPeacock $dir) { return $dir }
    }
    if ($AllowMissing) { return $null }
    throw 'Nessuna installazione Peacock PRONTA (packaged) trovata. Esegui prima ScaricaRelease.cmd / Get-OfficialRelease.ps1.'
}

function Find-HitmanExe {
    param([string]$Configured)
    $guesses = @(
        $Configured,
        'C:\Games\HITMAN - World of Assassination\Retail\HITMAN3.exe',
        'C:\Program Files (x86)\Steam\steamapps\common\HITMAN 3\Retail\HITMAN3.exe',
        'C:\Program Files\Epic Games\HITMAN3\Retail\HITMAN3.exe'
    )
    foreach ($g in $guesses) {
        if ($g -and (Test-Path -LiteralPath $g)) { return $g }
    }

    $steamPath = $null
    foreach ($key in @(
            'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam',
            'HKLM:\SOFTWARE\Valve\Steam'
        )) {
        try {
            $steamPath = (Get-ItemProperty -Path $key -ErrorAction Stop).InstallPath
            if ($steamPath) { break }
        } catch {}
    }
    if ($steamPath) {
        $libs = New-Object System.Collections.Generic.List[string]
        [void]$libs.Add((Join-Path $steamPath 'steamapps'))
        $vdf = Join-Path $steamPath 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $vdf) {
            foreach ($line in Get-Content -LiteralPath $vdf) {
                if ($line -match '"path"\s+"(.+)"') {
                    $p = $Matches[1] -replace '\\\\', '\'
                    [void]$libs.Add((Join-Path $p 'steamapps'))
                }
            }
        }
        foreach ($lib in $libs) {
            foreach ($rel in @(
                    'common\HITMAN 3\Retail\HITMAN3.exe',
                    'common\HITMAN World of Assassination\Retail\HITMAN3.exe',
                    'common\HITMAN - World of Assassination\Retail\HITMAN3.exe'
                )) {
                $exe = Join-Path $lib $rel
                if (Test-Path -LiteralPath $exe) { return $exe }
            }
        }
    }
    return $null
}

function Unblock-PeacockTree {
    param([string]$Dir)
    if (-not (Test-Path -LiteralPath $Dir)) { return }
    Get-ChildItem -LiteralPath $Dir -Recurse -File -ErrorAction SilentlyContinue |
        ForEach-Object {
            try { Unblock-File -LiteralPath $_.FullName -ErrorAction SilentlyContinue } catch {}
        }
}

function Get-LatestPeacockRelease {
    $api = 'https://api.github.com/repos/thepeacockproject/Peacock/releases/latest'
    try {
        $rel = Invoke-RestMethod -Uri $api -Headers @{ 'User-Agent' = 'PeacockOfflineKit' }
        $asset = $rel.assets | Where-Object { $_.name -eq "Peacock-$($rel.tag_name).zip" } | Select-Object -First 1
        if (-not $asset) {
            $asset = $rel.assets | Where-Object { $_.name -like 'Peacock-v*.zip' -and $_.name -notlike '*linux*' } | Select-Object -First 1
        }
        return [pscustomobject]@{
            Tag  = $rel.tag_name
            Name = $rel.name
            Url  = $asset.browser_download_url
            File = $asset.name
        }
    } catch {
        return [pscustomobject]@{
            Tag  = 'v8.9.1'
            Name = 'v8.9.1'
            Url  = 'https://github.com/thepeacockproject/Peacock/releases/download/v8.9.1/Peacock-v8.9.1.zip'
            File = 'Peacock-v8.9.1.zip'
        }
    }
}

function Test-HttpLocalhost {
    param([string]$HostName = '127.0.0.1', [int]$TimeoutSec = 2)
    try {
        $req = [System.Net.WebRequest]::Create("http://$HostName/")
        $req.Timeout = $TimeoutSec * 1000
        $req.Method = 'GET'
        $resp = $req.GetResponse()
        $resp.Close()
        return $true
    } catch {
        if ($_.Exception.InnerException -and $_.Exception.Message -match '200|OK|html') {
            return $true
        }
        # Connection refused vs connection made then HTTP error: latter still means server is up
        if ($_.Exception.Message -match '400|401|403|404|405|500') { return $true }
        return $false
    }
}

function Get-Port80Owner {
    try {
        $conns = Get-NetTCPConnection -LocalPort 80 -State Listen -ErrorAction Stop
        $pids = $conns | Select-Object -ExpandProperty OwningProcess -Unique
        $names = foreach ($pid in $pids) {
            try { (Get-Process -Id $pid -ErrorAction Stop).ProcessName + " (PID $pid)" } catch { "PID $pid" }
        }
        return ($names -join ', ')
    } catch {
        return $null
    }
}

function Set-OfflineFriendlyOptions {
    param([string]$PeacockDir)
    $ini = Join-Path $PeacockDir 'options.ini'
    if (-not (Test-Path -LiteralPath $ini)) {
        Write-WarnLine "options.ini non esiste ancora (verra creato al primo avvio del server)."
        return
    }
    $text = Get-Content -LiteralPath $ini -Raw
    $replacements = @{
        'updateChecking\s*=\s*\S+' = 'updateChecking=false'
        'leaderboards\s*=\s*\S+'    = 'leaderboards=false'
        'imageLoading\s*=\s*\S+'    = 'imageLoading=OFFLINE'
        'jokes\s*=\s*\S+'           = 'jokes=false'
    }
    foreach ($k in $replacements.Keys) {
        if ($text -match $k) {
            $text = [regex]::Replace($text, $k, $replacements[$k])
        }
    }
    Set-Content -LiteralPath $ini -Value $text -Encoding UTF8
    Write-Ok 'options.ini impostato per uso offline (niente check update, niente leaderboard, immagini OFFLINE).'
}

function Get-DefenderStatusForPath {
    param([string]$Path)
    $result = [pscustomobject]@{
        Available      = $false
        Excluded       = $false
        Threats        = @()
        QuarantinedHint = $false
    }
    try {
        $null = Get-Command Get-MpPreference -ErrorAction Stop
        $pref = Get-MpPreference -ErrorAction Stop
        $result.Available = $true
        if ($pref.ExclusionPath) {
            foreach ($ex in $pref.ExclusionPath) {
                if ($Path -and ($Path.StartsWith($ex, [StringComparison]::OrdinalIgnoreCase) -or $ex.StartsWith($Path, [StringComparison]::OrdinalIgnoreCase))) {
                    $result.Excluded = $true
                }
            }
        }
        try {
            $threats = Get-MpThreatDetection -ErrorAction SilentlyContinue
            if ($threats) {
                $result.Threats = @($threats | Where-Object {
                        $_.Resources -and ($_.Resources -join ' ') -match 'Peacock|Patcher'
                    })
                if ($result.Threats.Count -gt 0) { $result.QuarantinedHint = $true }
            }
        } catch {}
    } catch {
        $result.Available = $false
    }
    return $result
}

Export-ModuleMember -Function * -Alias *
