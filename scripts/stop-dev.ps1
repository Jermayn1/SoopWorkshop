#Requires -Version 5.1
<#
.SYNOPSIS
    Beendet Backend und Frontend der Entwicklungsumgebung.

.DESCRIPTION
    Beendet die dotnet-Prozesse von Backend und Frontend. Der PostgreSQL-Container
    bleibt standardmaessig laufen, damit die Daten erreichbar bleiben.

.PARAMETER StopDatabase
    Stoppt zusaetzlich den PostgreSQL-Container.

.EXAMPLE
    .\scripts\stop-dev.ps1

.EXAMPLE
    .\scripts\stop-dev.ps1 -StopDatabase
#>
[CmdletBinding()]
param(
    [switch]$StopDatabase,
    [string]$DbContainer = 'soopworkshop-db'
)

$ErrorActionPreference = 'Stop'

# Frontend.Web steht weiter in der Liste: wer das stillgelegte Frontend zum Vergleich
# noch einmal startet, soll es hiermit auch wieder loswerden.
$processNames = @('SoopWorkshop.Backend.API', 'SoopWorkshop.Frontend.Web')
$stopped = 0

foreach ($name in $processNames) {
    $processes = Get-Process -Name $name -ErrorAction SilentlyContinue
    if ($null -eq $processes) {
        Write-Host "$name laeuft nicht." -ForegroundColor DarkGray
        continue
    }

    foreach ($process in $processes) {
        Stop-Process -Id $process.Id -Force
        $stopped++
    }
    Write-Host "$name beendet." -ForegroundColor Green
}

if ($stopped -eq 0) {
    Write-Host 'Es lief nichts, was beendet werden musste.' -ForegroundColor DarkGray
}

if ($StopDatabase) {
    $dockerAvailable = $null -ne (Get-Command docker -ErrorAction SilentlyContinue)
    if ($dockerAvailable) {
        docker stop $DbContainer | Out-Null
        Write-Host "Container '$DbContainer' gestoppt." -ForegroundColor Green
    }
    else {
        Write-Host 'Docker nicht gefunden, Container nicht gestoppt.' -ForegroundColor Yellow
    }
}
