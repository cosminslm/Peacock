#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Ok([string]$Message) { Write-Host "[OK]    $Message" -ForegroundColor Green }
function Write-Info([string]$Message) { Write-Host "[INFO]  $Message" -ForegroundColor Cyan }
function Write-WarnLine([string]$Message) { Write-Host "[WARN]  $Message" -ForegroundColor Yellow }
function Write-ErrLine([string]$Message) { Write-Host "[ERRORE] $Message" -ForegroundColor Red }
function Write-Step([string]$Message) { Write-Host "`n=== $Message ===" -ForegroundColor Magenta }

function Get-LocaleRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }
    return (Get-Location).Path
}

function Read-LocaleConfig {
    $path = Join-Path (Get-LocaleRoot) 'config.json'
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Manca config.json in $(Get-LocaleRoot)"
    }
    return (Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function Test-IsAdministrator {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $p = New-Object Security.Principal.WindowsPrincipal($id)
    return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Request-Administrator {
    param([string]$ScriptPath)
    if (Test-IsAdministrator) { return $true }
    Write-WarnLine 'Servono privilegi amministratore (porta 80 + patcher). Rilancio elevato...'
    $argString = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$ScriptPath`"")
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
    if (-not $Dir -or -not (Test-Path -LiteralPath $Dir)) { return $false }
    return (Test-Path -LiteralPath (Join-Path $Dir 'chunk0.js')) -and
        (Test-Path -LiteralPath (Join-Path $Dir 'PeacockPatcher.exe')) -and
        (Test-Path -LiteralPath (Join-Path $Dir 'nodedist\node.exe'))
}

function Get-PlayerProfileFiles {
    param([string]$Dir)
    $users = Join-Path $Dir 'userdata\users'
    if (-not (Test-Path -LiteralPath $users)) { return @() }
    return @(Get-ChildItem -LiteralPath $users -Filter '*.json' -ErrorAction SilentlyContinue |
        Where-Object { $_.BaseName -ne 'lop' })
}

function Get-UserdataSummary {
    param([string]$Dir)
    $files = @(Get-PlayerProfileFiles $Dir)
    if ($files.Count -eq 0) { return 'nessun profilo giocatore' }
    $bits = $files | ForEach-Object {
        '{0} ({1} byte, {2})' -f $_.BaseName.Substring(0, [Math]::Min(8, $_.BaseName.Length)), $_.Length, $_.LastWriteTime.ToString('yyyy-MM-dd HH:mm')
    }
    return ("{0} profilo/i: {1}" -f $files.Count, ($bits -join ' | '))
}

function Get-OwnedWoaEntitlementIds {
    @(
        '1659040',
        '1829580', '1829581', '1829582', '1829583', '1829584', '1829585', '1829586',
        '1829590', '1829591', '1829595', '1829596', '1829605',
        '1843460', '2184790', '2184791',
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
        '28455871cd0d4ffab52f557cc012ea5e'
    )
}

function Restore-OwnedWoaEntitlements {
    param([string]$PeacockDir)
    $owned = Get-OwnedWoaEntitlementIds
    $files = @(Get-PlayerProfileFiles $PeacockDir)
    if ($files.Count -eq 0) {
        Write-Info 'Nessun profilo ancora. Dopo il primo hub si crea il JSON.'
        return
    }
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
        Write-Ok ("Profilo {0}: {1} entitlement." -f $f.BaseName.Substring(0, 8), $current.Count)
    }
}

function Set-OfflineFriendlyOptions {
    param([string]$PeacockDir)
    $ini = Join-Path $PeacockDir 'options.ini'
    if (-not (Test-Path -LiteralPath $ini)) {
        @('[peacock]', 'updateChecking=false', 'leaderboards=false', 'imageLoading=OFFLINE', 'jokes=false') |
            Set-Content -LiteralPath $ini -Encoding UTF8
        Write-Ok "Creato $ini"
        return
    }
    $text = Get-Content -LiteralPath $ini -Raw
    foreach ($pair in @{
            'updateChecking\s*=\s*\S+' = 'updateChecking=false'
            'leaderboards\s*=\s*\S+'    = 'leaderboards=false'
            'imageLoading\s*=\s*\S+'    = 'imageLoading=OFFLINE'
        }.GetEnumerator()) {
        if ($text -match $pair.Key) {
            $text = [regex]::Replace($text, $pair.Key, $pair.Value)
        }
    }
    Set-Content -LiteralPath $ini -Value $text -Encoding UTF8
    Write-Ok 'options.ini: niente update, niente leaderboard, immagini OFFLINE.'
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
    $want = [IO.Path]::GetFullPath($PeacockDir).TrimEnd('\')
    try {
        $conns = Get-NetTCPConnection -LocalPort 80 -State Listen -ErrorAction Stop
        foreach ($procId in @($conns | Select-Object -ExpandProperty OwningProcess -Unique)) {
            try {
                $path = (Get-Process -Id $procId -ErrorAction Stop).Path
                if ($path -and ([IO.Path]::GetFullPath($path)).StartsWith($want, [StringComparison]::OrdinalIgnoreCase)) {
                    return $true
                }
            } catch {}
        }
    } catch {}
    return $false
}

function Test-InternetReachable {
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $iar = $client.BeginConnect('1.1.1.1', 443, $null, $null)
        $ok = $iar.AsyncWaitHandle.WaitOne(1500, $false)
        $connected = $ok -and $client.Connected
        try { $client.Close() } catch {}
        return [bool]$connected
    } catch {
        return $false
    }
}

function Write-OfflineProbe {
    Write-Step 'Prova rete (come in viaggio)'
    if (Test-InternetReachable) {
        Write-WarnLine 'Internet ANCORA raggiungibile. Questa NON e una prova offline.'
        Write-Host 'Spegni Wi-Fi / stacca cavo / aereo, poi rilancia ProvaOffline.cmd.' -ForegroundColor Yellow
        return $false
    }
    Write-Ok 'Nessuna rete verso internet. Prova offline valida. Peacock usa solo 127.0.0.1.'
    return $true
}

Export-ModuleMember -Function * -Alias *
