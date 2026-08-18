#Requires -Version 5.1
<#
.SYNOPSIS
    Erzeugt das TLS-Zertifikat fuer den Reverse Proxy aus der eigenen CA.

.DESCRIPTION
    Kapselt den mkcert-Aufruf, damit die Erneuerung in zwei Jahren kein
    Raetselraten wird. Das Ergebnis landet unter certs/ und wird von dort in den
    Frontend-Container gemountet.

    Warum eine eigene CA und nicht selbstsigniert: ein selbstsigniertes
    Zertifikat erzeugt bei JEDEM Teilnehmer bei JEDEM Besuch eine ganzseitige
    Browser-Warnung. Das ist nicht nur unschoen - es bringt einem ganzen Kurs
    bei, Sicherheitswarnungen wegzuklicken. Mit einer eigenen CA faellt die
    Warnung weg; der Preis ist ein einmalig verteiltes Wurzelzertifikat.

    Die vollstaendige Anleitung steht in docs/server-aufsetzen.md, Abschnitt 6.

.PARAMETER Name
    Der DNS-Name, unter dem die Teilnehmer die Seite aufrufen.

.PARAMETER ServerIp
    Optional die IP der VM als zusaetzlicher Eintrag im Zertifikat. Damit ist
    auch der direkte Aufruf ohne Namen ohne Warnung moeglich - hilfreich, solange
    der DNS-Eintrag noch nicht steht.

.PARAMETER Ziel
    Ausgabeverzeichnis. Standard ist certs/ im Repository.

.EXAMPLE
    .\scripts\erzeuge-zertifikat.ps1 -Name soop.workshop -ServerIp 192.168.1.50

.EXAMPLE
    .\scripts\erzeuge-zertifikat.ps1 -Name soop.internal
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Name,
    [string]$ServerIp,
    [string]$Ziel
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $Ziel) { $Ziel = Join-Path $repoRoot 'certs' }

Write-Host ''
Write-Host "Zertifikat fuer $Name" -ForegroundColor White

# ---------------------------------------------------------------- mkcert ------
if (-not (Get-Command mkcert -ErrorAction SilentlyContinue)) {
    Write-Host ''
    Write-Host 'mkcert ist nicht installiert.' -ForegroundColor Red
    Write-Host 'Installieren mit:' -ForegroundColor Yellow
    Write-Host '    winget install FiloSottile.mkcert' -ForegroundColor Yellow
    Write-Host 'Danach einmalig die eigene CA anlegen:' -ForegroundColor Yellow
    Write-Host '    mkcert -install' -ForegroundColor Yellow
    exit 1
}

# Ohne installierte CA erzeugt mkcert zwar ein Zertifikat, aber keines, dem
# irgendein Browser traut - der Lauf saehe erfolgreich aus und waere wertlos.
$caRoot = (& mkcert -CAROOT).Trim()
if (-not (Test-Path (Join-Path $caRoot 'rootCA.pem'))) {
    Write-Host ''
    Write-Host 'Es gibt noch keine lokale CA.' -ForegroundColor Red
    Write-Host 'Einmalig anlegen mit:' -ForegroundColor Yellow
    Write-Host '    mkcert -install' -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $Ziel)) {
    New-Item -ItemType Directory -Path $Ziel | Out-Null
}

$zertifikat = Join-Path $Ziel 'soop.pem'
$schluessel = Join-Path $Ziel 'soop-key.pem'

# ------------------------------------------------------------- erzeugen -------
# localhost und 127.0.0.1 kommen mit: damit laesst sich der Aufbau auch auf der
# VM selbst pruefen, bevor der DNS-Eintrag steht.
$namen = @($Name, 'localhost', '127.0.0.1')
if ($ServerIp) { $namen += $ServerIp }

Write-Host "  Namen im Zertifikat: $($namen -join ', ')" -ForegroundColor DarkGray

& mkcert -cert-file $zertifikat -key-file $schluessel @namen
if ($LASTEXITCODE -ne 0) { throw 'mkcert ist fehlgeschlagen.' }

# ------------------------------------------------------------ Kontrolle -------
if (-not (Test-Path $zertifikat) -or -not (Test-Path $schluessel)) {
    throw "mkcert meldete Erfolg, aber $zertifikat oder $schluessel fehlt."
}

Write-Host ''
Write-Host 'Fertig.' -ForegroundColor Green
Write-Host "  Zertifikat:  $zertifikat"
Write-Host "  Schluessel:  $schluessel"
Write-Host ''
Write-Host 'Beide sind gitignoriert - ein privater Schluessel gehoert in kein Repository.' -ForegroundColor DarkGray
Write-Host ''
Write-Host 'Weiter mit docs/server-aufsetzen.md:' -ForegroundColor White
Write-Host '  6.3  beide Dateien nach certs/ auf die VM kopieren'
Write-Host '  6.4  das Wurzelzertifikat auf die Teilnehmerrechner bringen:'
Write-Host "         $(Join-Path $caRoot 'rootCA.pem')" -ForegroundColor Yellow
Write-Host ''
Write-Host 'Ohne Schritt 6.4 ist die Seite erreichbar, zeigt aber eine Warnung.' -ForegroundColor DarkGray
