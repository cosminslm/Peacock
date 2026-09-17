#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$module = Join-Path $PSScriptRoot 'lib\PeacockOffline.psm1'
Import-Module $module -Force

$dir = Resolve-PackagedPeacockDir
Set-OfflineFriendlyOptions $dir
Write-Host ''
Write-Host 'Nota: al primo avvio Peacock puo comunque tentare di leggere i DLC da Steam/IOI.' -ForegroundColor DarkGray
Write-Host 'Dopo un login riuscito, il profilo in userdata\ conserva gli unlock e puoi giocare offline.' -ForegroundColor DarkGray
if ($Host.Name -eq 'ConsoleHost') {
    Write-Host 'Premi un tasto per chiudere...' -ForegroundColor DarkGray
    [void][System.Console]::ReadKey($true)
}
