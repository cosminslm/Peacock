#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# Marks ICA + Paris (The Showstopper) as completed on the LOCAL Peacock
# profile so the next story beat is Sapienza (World of Tomorrow).
# Does NOT grant weapons/mastery. Close the game and Peacock first.

$module = Join-Path $PSScriptRoot 'lib\PeacockOffline.psm1'
Import-Module $module -Force

Write-Host ''
Write-Host '  SALTO: dopo la sfilata di Parigi  ->  prossima Sapienza' -ForegroundColor Magenta
Write-Host '  Chiudi HITMAN, patcher e server PRIMA di continuare.' -ForegroundColor Yellow
Write-Host ''

$peacock = Resolve-PackagedPeacockDir
Write-Info $peacock

$hitman = Get-Process -Name 'HITMAN3','HITMAN','node' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -like '*Peacock*' -or $_.Name -eq 'HITMAN3' }
if ($hitman) {
    Write-WarnLine 'Chiudi gioco e server Peacock, poi rilancia questo script.'
    if ($Host.Name -eq 'ConsoleHost') { [void][System.Console]::ReadKey($true) }
    exit 1
}

$usersDir = Join-Path $peacock 'userdata\users'
$files = @(Get-ChildItem -LiteralPath $usersDir -Filter '*.json' -ErrorAction SilentlyContinue)
if ($files.Count -eq 0) {
    Write-ErrLine "Nessun profilo in $usersDir"
    exit 1
}

$completed = @{
    '1d241b00-f585-4e3d-bc61-3095af1b96e2' = 'ICA addestramento guidato'
    'b573932d-7a34-44f1-bcf4-ea8f79f75710' = 'ICA addestramento libero'
    'ada5f2b1-8529-48bb-a596-717f75f5eacb' = 'ICA prova finale'
    '00000000-0000-0000-0000-000000000200' = 'Parigi - The Showstopper (sfilata)'
}

$now = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()

foreach ($f in $files) {
    $raw = Get-Content -LiteralPath $f.FullName -Raw -Encoding UTF8
    $bak = $f.FullName + '.pre-sfilata.bak'
    if (-not (Test-Path -LiteralPath $bak)) {
        Copy-Item -LiteralPath $f.FullName -Destination $bak
        Write-Info "Backup: $bak"
    }

    foreach ($id in $completed.Keys) {
        $blob = ('"' + $id + '": { "LastPlayedAt": ' + $now + ', "Completed": true }')
        if ($raw -match [regex]::Escape('"' + $id + '"')) {
            $raw = [regex]::Replace(
                $raw,
                '"' + [regex]::Escape($id) + '"\s*:\s*\{[^}]*\}',
                $blob,
                1
            )
        } else {
            if ($raw -match '"PeacockPlayedContracts"\s*:\s*\{\s*\}') {
                $raw = [regex]::Replace(
                    $raw,
                    '"PeacockPlayedContracts"\s*:\s*\{\s*\}',
                    ('"PeacockPlayedContracts": { ' + $blob + ' }'),
                    1
                )
            } elseif ($raw -match '"PeacockPlayedContracts"\s*:\s*\{') {
                $raw = [regex]::Replace(
                    $raw,
                    '"PeacockPlayedContracts"\s*:\s*\{',
                    ('"PeacockPlayedContracts": { ' + $blob + ','),
                    1
                )
            }
        }
        Write-Ok $completed[$id]
    }

    # Modest Paris XP so the destination does not look brand new (not max mastery).
    $raw = [regex]::Replace(
        $raw,
        '("LOCATION_PARENT_PARIS"\s*:\s*\{[^}]*?"Xp"\s*:\s*)0',
        '${1}8000',
        1
    )
    $raw = [regex]::Replace(
        $raw,
        '("LOCATION_PARENT_PARIS"\s*:\s*\{[^}]*?"Level"\s*:\s*)1',
        '${1}2',
        1
    )

    Set-Content -LiteralPath $f.FullName -Value $raw -Encoding UTF8
    Write-Ok ("Profilo aggiornato: " + $f.Name)
}

Write-Host ''
Write-Host 'Test locale: solo Gioca.cmd (admin). Se la copia e Epic: Epic Launcher aperto e loggato (Steam non serve).' -ForegroundColor Cyan
Write-Host 'Nel Hub: Parigi deve risultare giocata. Prossima storia: Sapienza (World of Tomorrow).' -ForegroundColor Cyan
Write-Host 'Niente Carica/Continua. Planning -> Start su Sapienza.' -ForegroundColor Cyan
Write-Host ''
if ($Host.Name -eq 'ConsoleHost') {
    Write-Host 'Premi un tasto...' -ForegroundColor DarkGray
    [void][System.Console]::ReadKey($true)
}
