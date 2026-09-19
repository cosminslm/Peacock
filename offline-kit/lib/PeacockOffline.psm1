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
        peacockDir           = 'C:\Games\HITMAN - World of Assassination\Peacock'
        gameExe              = 'C:\Games\HITMAN - World of Assassination\Retail\HITMAN3.exe'
        serverUrl            = '127.0.0.1'
        launchGame           = $true
        stopOnGameExit       = $false
        applyOfflineOptions  = $true
        preferredInstallDir  = 'C:\Games\HITMAN - World of Assassination\Peacock'
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

function Get-PlayerProfileFiles {
    param([string]$Dir)
    $users = Join-Path $Dir 'userdata\users'
    if (-not (Test-Path -LiteralPath $users)) { return @() }
    return @(Get-ChildItem -LiteralPath $users -Filter '*.json' -ErrorAction SilentlyContinue |
        Where-Object { $_.BaseName -ne 'lop' })
}

function Get-UserdataScore {
    param([string]$Dir)
    $files = @(Get-PlayerProfileFiles $Dir)
    if ($files.Count -eq 0) { return 0 }
    $bytes = ($files | Measure-Object -Property Length -Sum).Sum
    return [int](1000 + $files.Count * 10 + [Math]::Min($bytes, 5000000) / 1000)
}

function Get-UserdataSummary {
    param([string]$Dir)
    $files = @(Get-PlayerProfileFiles $Dir)
    if ($files.Count -eq 0) { return 'nessun profilo giocatore in userdata\users' }
    $bits = $files | ForEach-Object {
        '{0} ({1} byte, {2})' -f $_.BaseName.Substring(0, [Math]::Min(8, $_.BaseName.Length)), $_.Length, $_.LastWriteTime.ToString('yyyy-MM-dd HH:mm')
    }
    return ("{0} profilo/i: {1}" -f $files.Count, ($bits -join ' | '))
}

function Write-AllPeacockCopies {
    Write-Step 'Tutte le copie Peacock su questo PC'
    $n = 0
    foreach ($dir in (Get-CandidatePeacockDirs)) {
        if (-not (Test-IsPackagedPeacock $dir)) { continue }
        $n++
        $ver = Get-PeacockVersionFromDir $dir
        Write-Host ("  [{0}] v{1}" -f $n, $ver)
        Write-Host ("       {0}" -f $dir)
        Write-Host ("       {0}" -f (Get-UserdataSummary $dir))
    }
    if ($n -gt 1) {
        Write-WarnLine 'C e PIU DI UNA copia. I salvataggi stanno SOLO nella cartella del server avviato. Le altre sono ignorate.'
    }
    if ($n -eq 0) { Write-Info 'Nessuna release packaged trovata.' }
}

function Resolve-PackagedPeacockDir {
    param([switch]$AllowMissing)
    $cfg = Read-OfflineConfig
    $pinned = @(
        $cfg.peacockDir,
        'C:\Games\HITMAN - World of Assassination\Peacock'
    )
    foreach ($dir in $pinned) {
        if ($dir -and (Test-IsPackagedPeacock $dir)) { return $dir }
    }

    $packaged = @()
    foreach ($dir in (Get-CandidatePeacockDirs)) {
        if (Test-IsPackagedPeacock $dir) { $packaged += $dir }
    }
    if ($packaged.Count -eq 0) {
        if ($AllowMissing) { return $null }
        throw 'Nessuna installazione Peacock PRONTA (packaged) trovata.'
    }

    $ranked = $packaged | Sort-Object -Property @{ Expression = { Get-UserdataScore $_ } } -Descending
    return ($ranked | Select-Object -First 1)
}

function Get-ProtectedPeacockNames {
    @(
        'userdata',
        'contractSessions',
        'options.ini',
        'images',
        'logs',
        'plugins',
        'contracts',
        'config.json'
    )
}

function Install-OfficialPeacockRelease {
    param(
        [string]$DestRoot,
        [switch]$Force
    )
    $keep = Get-ProtectedPeacockNames

    if (-not $Force -and (Test-IsPackagedPeacock $DestRoot)) {
        Write-Ok "Release gia presente, non riscarico: $DestRoot"
        Write-Info (Get-UserdataSummary $DestRoot)
        return $false
    }

    $rel = Get-LatestPeacockRelease
    Write-Info "Release da installare: $($rel.Tag)  $($rel.File)"
    $tmp = Join-Path $env:TEMP $rel.File
    Write-Host "Scarico $($rel.Url) ..."
    Invoke-WebRequest -Uri $rel.Url -OutFile $tmp -UseBasicParsing
    Write-Ok "Scaricato $((Get-Item $tmp).Length) byte"

    if (-not (Test-Path -LiteralPath $DestRoot)) {
        New-Item -ItemType Directory -Path $DestRoot | Out-Null
    }

    $extractTo = Join-Path $env:TEMP ("peacock-extract-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $extractTo | Out-Null
    Expand-Archive -LiteralPath $tmp -DestinationPath $extractTo -Force
    $inner = Get-ChildItem -LiteralPath $extractTo -Directory | Select-Object -First 1
    if (-not $inner) { throw 'ZIP senza cartella interna.' }

    Get-ChildItem -LiteralPath $inner.FullName | ForEach-Object {
        if ($keep -contains $_.Name) {
            Write-Info "Conservo $($_.Name) locale (progressi / opzioni)."
            return
        }
        $target = Join-Path $DestRoot $_.Name
        if (Test-Path -LiteralPath $target) {
            Remove-Item -LiteralPath $target -Recurse -Force
        }
        Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force
    }

    Remove-Item -LiteralPath $extractTo -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
    Unblock-PeacockTree $DestRoot

    if (-not (Test-IsPackagedPeacock $DestRoot)) {
        throw "Estrazione incompleta in $DestRoot"
    }
    Write-Ok "Installato $($rel.Tag) in $DestRoot (userdata intatto)."
    return $true
}

function Ensure-PackagedPeacock {
    $existing = Resolve-PackagedPeacockDir -AllowMissing
    if ($existing) {
        Write-Ok "Uso Peacock gia installato (niente download)."
        Write-Info $existing
        Write-Info (Get-UserdataSummary $existing)
        $cfg = Read-OfflineConfig
        if ($cfg.peacockDir -ne $existing) {
            $cfg.peacockDir = $existing
            Save-OfflineConfig $cfg
        }
        return $existing
    }

    $cfg = Read-OfflineConfig
    $dest = $cfg.preferredInstallDir
    if (-not $dest -or $dest.Trim() -eq '') {
        $dest = Join-Path $env:USERPROFILE 'Documents\Peacock'
    }
    Write-WarnLine "Nessuna release packaged. Download UNA TANTUM in $dest"
    [void](Install-OfficialPeacockRelease -DestRoot $dest)
    $cfg.peacockDir = $dest
    $cfg.preferredInstallDir = $dest
    Save-OfflineConfig $cfg
    return $dest
}

function Get-OwnedWoaEntitlementIds {
    # Steam app + DLC ids AND Epic GUIDs that Peacock checks in inventory.ts.
    # This restores PACK OWNERSHIP (maps/modes visible). It does NOT grant
    # mastery unlocks, suits, or freelancer upgrades.
    @(
        '1659040',
        '1829580', '1829581', '1829582', '1829583', '1829584', '1829585', '1829586',
        '1829590', '1829591', '1829595', '1829596', '1829605',
        '1843460',
        '2184790', '2184791',
        '2828470', '2973650', '3110360', '3254350', '3711140',
        '3957470', '4097630', '4328240', '4542910', '4621250', '4911210', '4944070',
        '06d4d61bbb774ca99c1661bee04fbde0',
        '2e4ad3e9aa9b4dcfa709b3f3b44cbf94',
        'a9b1afdd05584441aeec75ba230b2e54',
        '66246e4364134f4689da72e9c6731687',
        '4216cdf59dbc4f19af227be076520202',
        '8a690003855745e884d5040c6bc9ede8',
        'bc610b36c75442299edcbe99f6f0fb60',
        '5d06a6c6af9b4875b3530d5328f61287',
        '0b59243cb8aa420691b66be1ecbe68c0',
        '894d1e6771044f48a8fdde934b8e443a',
        'e698e1a4b63947b0bc9349a5ae2dc015',
        '391d08a543dc43a083eb50246916a291',
        'afa4b921503f43339c360d4b53910791',
        '6408de14f7dc46b9a33adcf6cbc4d159',
        'a3509775467d4d6a8a7adffe518dc204',
        '84a1a6fda4fb48afbb78ee9b2addd475',
        '08d2bc4d20754191b6c488541d2b4fa1',
        'a1e9a63fa4f3425aa66b9b8fa3c9cc35',
        '28455871cd0d4ffab52f557cc012ea5e',
        '0e8632b4cdfb415e94291d97d727b98d',
        '3f9adc216dde44dda5e829f11740a0a2',
        'aece009ff59441c0b526f8aa69e24cfb',
        'dfe5aeb89976450ba1e0e2c208b63d33',
        '30107bff80024d1ab291f9cd3bac9fac',
        '9e936ed2507a473db6f53ad24d2da587',
        '0403062df0d347619c8dcf043c65c02e',
        '9220c020262f420da06eb46a4b1ce86f',
        '6cdf07da030d4f66acd50eaf3cd234c7',
        'f04198e0ffcf49079b5ec77bb6b66891',
        '70a9afcc8de84b6ab0f2b45b2018559b',
        '256eeeb3d8044aa1840e1606d268e0b2',
        '04cb1b3e5b424308be25236f6bc1b2fb',
        '0047ddcd5e6846e881f1037c1416e3d9',
        'b135c766d25948c39d7dd316dbc4db53',
        '16bcef4f91674b00ba3d7f2d4f629cec',
        'd51a3a65928841d5b4cabad20a865006',
        '7b3bf47c436644ea8fea4f95317d431c',
        'd396d245e23a401b8422116db9026a27'
    )
}

function Restore-OwnedWoaEntitlements {
    param([string]$PeacockDir)
    $usersDir = Join-Path $PeacockDir 'userdata\users'
    if (-not (Test-Path -LiteralPath $usersDir)) {
        Write-Info 'Nessun profilo userdata ancora. Avvia una volta il gioco, chiudi, rilancia Gioca.cmd.'
        return
    }
    $owned = Get-OwnedWoaEntitlementIds
    $files = @(Get-PlayerProfileFiles $PeacockDir)
    foreach ($f in $files) {
        $raw = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8
        $current = New-Object System.Collections.Generic.List[string]
        $m = [regex]::Match($raw, '"entP"\s*:\s*\[(?<body>[\s\S]*?)\]')
        if ($m.Success) {
            [regex]::Matches($m.Groups['body'].Value, '"([^"]+)"') | ForEach-Object {
                [void]$current.Add($_.Groups[1].Value)
            }
        }
        foreach ($id in $owned) {
            if (-not $current.Contains($id)) { [void]$current.Add($id) }
        }
        $idsJson = ($current | ForEach-Object { '            "' + $_ + '"' }) -join ",`r`n"
        $entBlock = '"entP": [' + "`r`n" + $idsJson + "`r`n        ]"
        if ($m.Success) {
            $raw = $raw.Remove($m.Index, $m.Length).Insert($m.Index, $entBlock)
        } elseif ($raw -match '"Extensions"\s*:\s*\{') {
            $raw = [regex]::Replace($raw, '("Extensions"\s*:\s*\{)', ('$1' + "`r`n        " + $entBlock + ','), 1)
        } else {
            Write-WarnLine "Profilo $($f.Name): Extensions assente, salto."
            continue
        }
        $raw = [regex]::Replace($raw, '"IsFSPUser"\s*:\s*true', '"IsFSPUser": false')
        $bak = $f.FullName + '.bak'
        if (-not (Test-Path -LiteralPath $bak)) {
            Copy-Item -LiteralPath $f.FullName -Destination $bak
        }
        Set-Content -LiteralPath $f.FullName -Value $raw -Encoding UTF8
        Write-Ok ("Profilo {0}: {1} entitlement (mappe/modi visibili; mastery invariata)." -f $f.BaseName.Substring(0, 8), $current.Count)
    }
}

function Try-AddDefenderExclusion {
    param([string]$Dir)
    if (-not (Test-IsAdministrator)) { return }
    try {
        $null = Get-Command Add-MpPreference -ErrorAction Stop
        Add-MpPreference -ExclusionPath $Dir -ErrorAction Stop
        Write-Ok "Esclusione Defender: $Dir"
    } catch {
        Write-Info "Defender non disponibile o AV di terze parti: aggiungi a mano l'esclusione su PeacockPatcher.exe."
    }
}

function Get-HitmanPlatform {
    param([string]$Exe)
    $result = [pscustomobject]@{
        Kind        = 'sconosciuta'
        Supported   = $true
        NeedProcess = $null
        Hint        = 'Piattaforma non riconosciuta dai file accanto a HITMAN3.exe.'
        Exe         = $Exe
    }
    if (-not $Exe -or -not (Test-Path -LiteralPath $Exe)) { return $result }

    $dir = Split-Path -Parent $Exe
    $root = Split-Path -Parent $dir
    $probe = @($dir, $root)
    $hasFile = {
        param([string]$Name)
        foreach ($p in $probe) {
            if (Test-Path -LiteralPath (Join-Path $p $Name)) { return $true }
        }
        return $false
    }

    $hasSteam = (& $hasFile 'steam_api64.dll') -or (& $hasFile 'steam_api.dll')
    $hasEos = (& $hasFile 'EOSSDK-Win64-Shipping.dll')
    $hasMs = (& $hasFile 'MicrosoftGame.config') -or (& $hasFile 'appxmanifest.xml')
    if ($Exe -match 'XboxGames|WindowsApps|Program Files\\WindowsApps') { $hasMs = $true }

    if ($hasMs -and -not $hasEos -and -not $hasSteam) {
        $result.Kind = 'gamepass'
        $result.Supported = $false
        $result.Hint = 'Copia Xbox Game Pass / Microsoft Store. Peacock NON la supporta (file cifrati, il patcher non aggancia). Usa la copia Epic, non quella dell''app Xbox.'
        return $result
    }
    if ($hasEos -and -not $hasSteam) {
        $result.Kind = 'epic'
        $result.NeedProcess = 'EpicGamesLauncher'
        $result.Hint = 'Copia Epic. Steam NON serve. Lascia Epic Games Launcher aperto e loggato (internet al login). Poi Gioca.cmd.'
        return $result
    }
    if ($hasSteam) {
        $result.Kind = 'steam'
        $result.NeedProcess = 'steam'
        $result.Hint = 'Copia Steam. Lascia Steam aperto e loggato (internet al login).'
        return $result
    }
    if ($hasEos) {
        $result.Kind = 'epic'
        $result.NeedProcess = 'EpicGamesLauncher'
        $result.Hint = 'Copia Epic. Steam NON serve. Lascia Epic Games Launcher aperto e loggato.'
        return $result
    }
    return $result
}

function Test-PlatformLauncherRunning {
    param($Platform)
    if (-not $Platform -or -not $Platform.NeedProcess) { return $true }
    $names = @($Platform.NeedProcess)
    if ($Platform.Kind -eq 'epic') { $names += 'EpicWebHelper' }
    $proc = Get-Process -Name $names -ErrorAction SilentlyContinue
    return [bool]$proc
}

function Find-HitmanExe {
    param([string]$Configured)
    $guesses = @(
        $Configured,
        'C:\Games\HITMAN - World of Assassination\Retail\HITMAN3.exe',
        'C:\Program Files (x86)\Steam\steamapps\common\HITMAN 3\Retail\HITMAN3.exe',
        'C:\Program Files\Epic Games\HITMAN3\Retail\HITMAN3.exe',
        'C:\Program Files\Epic Games\HITMAN World of Assassination\Retail\HITMAN3.exe',
        'C:\Program Files\Epic Games\HITMANWOA\Retail\HITMAN3.exe'
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

function Test-Port80IsThisPeacock {
    param([string]$PeacockDir)
    if (-not $PeacockDir) { return $false }
    $want = [IO.Path]::GetFullPath($PeacockDir).TrimEnd('\')
    try {
        $conns = Get-NetTCPConnection -LocalPort 80 -State Listen -ErrorAction Stop
        $pids = @($conns | Select-Object -ExpandProperty OwningProcess -Unique)
        foreach ($procId in $pids) {
            try {
                $proc = Get-Process -Id $procId -ErrorAction Stop
                $path = $null
                try { $path = $proc.Path } catch {}
                if (-not $path) { continue }
                $full = [IO.Path]::GetFullPath($path)
                if ($full.StartsWith($want, [StringComparison]::OrdinalIgnoreCase)) { return $true }
            } catch {}
        }
    } catch {}
    return $false
}

function Set-OfflineFriendlyOptions {
    param([string]$PeacockDir)
    $ini = Join-Path $PeacockDir 'options.ini'
    if (-not (Test-Path -LiteralPath $ini)) {
        @(
            '[peacock]'
            'updateChecking=false'
            'leaderboards=false'
            'imageLoading=OFFLINE'
            'jokes=false'
        ) | Set-Content -LiteralPath $ini -Encoding UTF8
        Write-Ok "Creato $ini (niente update, niente leaderboard, immagini OFFLINE)."
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
