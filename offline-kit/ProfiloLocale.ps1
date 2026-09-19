#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# Mostra IL file del profilo locale e scrive le uniche righe
# che Peacock capisce (options.ini). Non tocca HITMAN3.exe.

$module = Join-Path $PSScriptRoot 'lib\PeacockOffline.psm1'
Import-Module $module -Force

Write-Host ''
Write-Host '  PROFILO LOCALE  (niente Steam, niente IOI Continua)' -ForegroundColor Magenta
Write-Host ''

$peacock = Resolve-PackagedPeacockDir
Write-Ok $peacock

Write-Step '1. Le uniche righe da scrivere: options.ini di Peacock'
Set-OfflineFriendlyOptions $peacock
$ini = Join-Path $peacock 'options.ini'
Write-Host "File:" -ForegroundColor Yellow
Write-Host "  $ini"
Write-Host 'Righe (gia scritte da questo script / da Gioca.cmd):' -ForegroundColor Yellow
Write-Host '  updateChecking=false'
Write-Host '  leaderboards=false'
Write-Host '  imageLoading=OFFLINE'
Write-Host 'NON si scrive niente dentro HITMAN3.exe ne nella cartella del gioco.' -ForegroundColor DarkGray

Write-Step '2. Pacchetti visibili sul profilo (senza sbloccare armi)'
Restore-OwnedWoaEntitlements $peacock

Write-Step '3. Il salvataggio e QUESTO file JSON'
$usersDir = Join-Path $peacock 'userdata\users'
if (-not (Test-Path -LiteralPath $usersDir)) {
    Write-WarnLine "Ancora nessun profilo. Avvia UNA volta Gioca.cmd, entra in hub, esci, aspetta 10s."
    Write-Host "Poi qui comparira: $usersDir\<guid>.json"
} else {
    $files = @(Get-ChildItem -LiteralPath $usersDir -Filter '*.json' -ErrorAction SilentlyContinue)
    if ($files.Count -eq 0) {
        Write-WarnLine "Cartella vuota: $usersDir"
    } else {
        foreach ($f in $files) {
            Write-Ok $f.FullName
            Write-Info ("dimensione {0} byte  modificato {1}" -f $f.Length, $f.LastWriteTime.ToString('yyyy-MM-dd HH:mm'))
            $note = Join-Path $f.DirectoryName ($f.BaseName + '.LEGGIMI.txt')
            @(
                'Questo JSON E il tuo profilo HITMAN su Peacock.'
                'Non si "carica" dal menu Continua/Carica del gioco: quello e IOI.'
                'Ogni volta che fai Gioca.cmd (admin) + patcher, Peacock usa AUTOMATICAMENTE questo file.'
                'I progressi si riscrivono qui ogni ~3 secondi mentre giochi.'
                'Cartella Peacock da non cambiare: ' + $peacock
            ) | Set-Content -LiteralPath $note -Encoding UTF8
            Write-Info "Promemoria: $note"
        }
    }
}

Write-Host ''
Write-Host 'Come si usa, sempre uguale:' -ForegroundColor Cyan
Write-Host '  1. Chiudi il gioco. Non toccare Continua/Carica.'
Write-Host '  2. Gioca.cmd tasto destro, amministratore.'
Write-Host '  3. Hub -> Planning -> Start. I dati stanno nel JSON sopra.'
Write-Host 'Steam / Epic / Xbox NON sono il salvataggio. Il salvataggio e solo quel JSON.'
Write-Host ''
if ($Host.Name -eq 'ConsoleHost') {
    Write-Host 'Premi un tasto...' -ForegroundColor DarkGray
    [void][System.Console]::ReadKey($true)
}
