#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$module = Join-Path $PSScriptRoot 'lib\PeacockOffline.psm1'
Import-Module $module -Force

Write-Host ''
Write-Host '  PEACOCK / HITMAN WOA  -  DIAGNOSTICA OFFLINE' -ForegroundColor Magenta
Write-Host '  Nessuna modifica al sistema: solo lettura.' -ForegroundColor DarkGray
Write-Host ''

$cfg = Read-OfflineConfig
$issues = New-Object System.Collections.Generic.List[string]
$okCount = 0

Write-Step '1. Privilegi'
if (Test-IsAdministrator) {
    Write-Ok 'Sessione con privilegi amministratore.'
    $okCount++
} else {
    Write-WarnLine 'NON sei amministratore. Il patcher e il server (porta 80) richiedono Run as administrator.'
    [void]$issues.Add('Lancia i .cmd / PowerShell come amministratore.')
}

Write-Step '2. Installazioni Peacock trovate'
$foundPackaged = $false
$foundSource = $false
$versions = @()
foreach ($dir in (Get-CandidatePeacockDirs)) {
    if (-not (Test-Path -LiteralPath $dir)) { continue }
    $ver = Get-PeacockVersionFromDir $dir
    if (Test-IsPackagedPeacock $dir) {
        Write-Ok "PACKAGED  v$ver  ->  $dir"
        $foundPackaged = $true
        $versions += $ver
        $okCount++
    } elseif (Test-IsSourceCheckout $dir) {
        Write-WarnLine "SORGENTE (non giocabile)  v$ver  ->  $dir"
        Write-Host "         Manca chunk0.js / nodedist. Il pulsante GitHub 'Download ZIP' NON e una release." -ForegroundColor DarkYellow
        $foundSource = $true
        [void]$issues.Add("Hai il codice sorgente in $dir. Serve Peacock-vX.Y.Z.zip dalla pagina Releases.")
    } else {
        Write-Info "Cartella presente ma incompleta: $dir (versione $ver)"
    }
}
if (-not $foundPackaged) {
    Write-ErrLine 'Nessuna release packaged trovata (chunk0.js + nodedist + PeacockPatcher.exe).'
    [void]$issues.Add('Esegui ScaricaRelease.cmd per scaricare la release ufficiale.')
}

$unique = $versions | Select-Object -Unique
if ($unique.Count -gt 1) {
    Write-WarnLine ("Mismatch di versione: " + ($unique -join '  vs  '))
    Write-Host '         Server e patcher DEVONO provenire dalla stessa release.' -ForegroundColor DarkYellow
    [void]$issues.Add('Non mischiare Peacock 6.x con il patcher 8.x. Usa una sola release packaged.')
}

Write-Step '3. Patcher'
$packaged = $null
try { $packaged = Resolve-PackagedPeacockDir -AllowMissing } catch {}
$patcherCandidates = @()
if ($packaged) { $patcherCandidates += (Join-Path $packaged 'PeacockPatcher.exe') }
$patcherCandidates += @(
    (Join-Path $PSScriptRoot '..\PeacockPatcher.exe'),
    'C:\Games\HITMAN - World of Assassination\Peacock\PeacockPatcher.exe'
)
$seen = @{}
foreach ($p in $patcherCandidates) {
    if (-not $p -or $seen.ContainsKey($p)) { continue }
    $seen[$p] = $true
    if (Test-Path -LiteralPath $p) {
        $item = Get-Item -LiteralPath $p
        $zone = $false
        try {
            $ads = Get-Item -LiteralPath $p -Stream Zone.Identifier -ErrorAction SilentlyContinue
            if ($ads) { $zone = $true }
        } catch {}
        $msg = "Trovato $($item.Name)  $($item.Length) byte  $($item.LastWriteTime.ToString('yyyy-MM-dd'))  $p"
        if ($zone) {
            Write-WarnLine "$msg  [BLOCCATO da Zone.Identifier - Unblock-File necessario]"
            [void]$issues.Add("Sblocca $p (Mark of the Web). Il launcher lo fa in automatico.")
        } else {
            Write-Ok $msg
            $okCount++
        }
    }
}

Write-Step '4. Microsoft Defender'
$probePath = if ($packaged) { $packaged } else { $PSScriptRoot }
$def = Get-DefenderStatusForPath $probePath
if (-not $def.Available) {
    Write-Info 'Cmdlet Defender non disponibili (ok se usi altro AV).'
} else {
    if ($def.Excluded) {
        Write-Ok "Percorso in esclusione Defender: $probePath"
        $okCount++
    } else {
        Write-WarnLine "Nessuna esclusione Defender per $probePath. PeacockPatcher.exe e un falso positivo cronico."
        [void]$issues.Add('Esegui EsclusioneDefender.cmd (come admin) prima di lanciare il patcher.')
    }
    if ($def.QuarantinedHint) {
        Write-ErrLine 'Rilevate minacce/quarantena che citano Peacock/Patcher. Controlla Protezione di Windows -> Cronologia.'
        [void]$issues.Add('Ripristina PeacockPatcher.exe dalla quarantena Defender dopo aver aggiunto l''esclusione.')
    }
}

Write-Step '5. HITMAN World of Assassination'
$exe = Find-HitmanExe $cfg.gameExe
if ($exe) {
    $gi = Get-Item -LiteralPath $exe
    Write-Ok "$($gi.FullName)"
    Write-Info "Ultima modifica EXE: $($gi.LastWriteTime.ToString('yyyy-MM-dd HH:mm'))  size=$($gi.Length)"
    if ($gi.LastWriteTime.Year -le 2023) {
        Write-WarnLine 'Build del gioco del 2023. Funziona con Peacock 6.5.x; con 8.9.x il patcher usa AOB scan (di solito ok).'
        Write-Host '         Consiglio: aggiorna il gioco (Steam/Epic) e usa SOLO Peacock 8.9.1 packaged.' -ForegroundColor DarkYellow
    }
    $okCount++
} else {
    Write-ErrLine "HITMAN3.exe non trovato. Imposta gameExe in $(Get-ConfigPath)"
    [void]$issues.Add('Copia config.example.json in config.json e indica il percorso di HITMAN3.exe.')
}

Write-Step '6. Porta 80 (server Peacock)'
$owner = Get-Port80Owner
if ($owner) {
    Write-WarnLine "Porta 80 occupata da: $owner"
    if ($owner -match 'node') {
        Write-Info 'Probabilmente Peacock e gia in esecuzione. Ok se e il server che vuoi usare.'
    } else {
        [void]$issues.Add('Libera la porta 80 (IIS, Skype, altro web server) oppure ferma il processo indicato.')
    }
} else {
    Write-Ok 'Porta 80 libera.'
    $okCount++
}

Write-Step '7. Cosa NON fare'
Write-Host '  - NON usare Peacock-master.zip (sorgente). Serve Peacock-vX.Y.Z.zip da GitHub Releases.'
Write-Host '  - NON mischiare patcher 8.9.1 con server 6.5.1.'
Write-Host '  - NON mettere Peacock in Program Files ne dentro la cartella del gioco.'
Write-Host '  - NON lanciare il patcher senza admin.'
Write-Host '  - NON aspettarti che il patcher da solo avvii il server.'

Write-Step 'Esito'
if ($issues.Count -eq 0) {
    Write-Ok "Nessun blocco. Puoi lanciare Gioca.cmd"
} else {
    Write-ErrLine "$($issues.Count) problema/i da risolvere:"
    $n = 1
    foreach ($i in $issues) {
        Write-Host "  $n. $i"
        $n++
    }
    Write-Host ''
    Write-Host 'Ordine consigliato:' -ForegroundColor Cyan
    Write-Host '  1. ScaricaRelease.cmd     (release ufficiale packaged, NON il source)'
    Write-Host '  2. EsclusioneDefender.cmd (come amministratore)'
    Write-Host '  3. Gioca.cmd              (server + patcher + gioco)'
}

Write-Host ''
Write-Host 'Invia questo output se chiedi aiuto. Nessun file e stato modificato.' -ForegroundColor DarkGray
Write-Host ''
if ($Host.Name -eq 'ConsoleHost') {
    Write-Host 'Premi un tasto per chiudere...' -ForegroundColor DarkGray
    [void][System.Console]::ReadKey($true)
}
