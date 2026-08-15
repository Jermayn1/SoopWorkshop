#Requires -Version 5.1
<#
.SYNOPSIS
    Gleicht das Passwort der laufenden Datenbank an POSTGRES_PASSWORD aus der .env an.

.DESCRIPTION
    POSTGRES_PASSWORD wirkt nur beim ersten Anlegen des Volumes. Wird der Wert in
    der .env spaeter geaendert, behaelt die Datenbank ihr altes Passwort - der
    Fehler kommt dann als "28P01 password authentication failed" zurueck und
    sieht aus wie ein Tippfehler, obwohl beide Seiten fuer sich stimmen.

    Dieses Skript setzt das Passwort in der Datenbank per ALTER USER auf den Wert
    aus der .env und prueft anschliessend, ob die Anmeldung wirklich funktioniert.

    Die Pruefung laeuft bewusst von AUSSERHALB des Containers: innerhalb gilt
    'trust' aus der pg_hba.conf, dort wird jedes Passwort akzeptiert. Eine dort
    "bestaetigte" Uebereinstimmung sagt nichts aus.

    Die Alternative ist, das Volume neu aufzusetzen - das loescht aber alle Daten:
        docker compose down -v; docker compose up -d

.EXAMPLE
    .\scripts\sync-db-password.ps1
#>
[CmdletBinding()]
param(
    [string]$DbContainer = 'soopworkshop-db'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $repoRoot '.env'

function Write-Step {
    param([string]$Text)
    Write-Host ''
    Write-Host "==> $Text" -ForegroundColor Cyan
}

# Liest einen Schluessel aus der .env. Kommentare und Leerzeilen werden uebergangen.
function Get-EnvValue {
    param([string]$Key, [string]$Default = '')

    foreach ($line in Get-Content $envFile) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#')) { continue }

        $separator = $trimmed.IndexOf('=')
        if ($separator -le 0) { continue }

        if ($trimmed.Substring(0, $separator).Trim() -eq $Key) {
            return $trimmed.Substring($separator + 1).Trim().Trim('"').Trim("'")
        }
    }

    return $Default
}

if (-not (Test-Path $envFile)) {
    throw "Keine .env gefunden. Vorlage kopieren: cp .env.example .env"
}

$user = Get-EnvValue -Key 'POSTGRES_USER' -Default 'postgres'
$database = Get-EnvValue -Key 'POSTGRES_DB' -Default 'soopworkshop'
$password = Get-EnvValue -Key 'POSTGRES_PASSWORD'

if ([string]::IsNullOrWhiteSpace($password)) {
    throw 'POSTGRES_PASSWORD ist in der .env nicht gesetzt.'
}

$running = docker ps --filter "name=$DbContainer" --filter 'status=running' --format '{{.Names}}'
if ($running -ne $DbContainer) {
    throw "Container '$DbContainer' laeuft nicht. Zuerst 'docker compose up -d' aufrufen."
}

Write-Step "Passwort von '$user' auf den Wert aus der .env setzen"

# Ueber die Standardeingabe, damit das Passwort nicht in der Prozessliste landet.
"ALTER USER $user WITH PASSWORD '$password';" | docker exec -i $DbContainer psql -U $user -d $database -q
if ($LASTEXITCODE -ne 0) {
    throw 'ALTER USER ist fehlgeschlagen.'
}

Write-Host '    Gesetzt.' -ForegroundColor Green

Write-Step 'Anmeldung von ausserhalb des Containers pruefen'

$image = 'postgres:16-alpine'
$check = docker run --rm -e PGPASSWORD="$password" $image `
    psql -h host.docker.internal -p 5432 -U $user -d $database -tc 'select 1;' 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host '    Anmeldung erfolgreich - .env und Datenbank passen zusammen.' -ForegroundColor Green
    Write-Host ''
    Write-Host '    Ein laufendes Backend muss nicht neu gestartet werden:' -ForegroundColor DarkGray
    Write-Host '    der Connection-String hat sich nicht geaendert, nur das Passwort dahinter.' -ForegroundColor DarkGray
    Write-Host ''
}
else {
    Write-Host '    Anmeldung fehlgeschlagen:' -ForegroundColor Red
    Write-Host "    $check" -ForegroundColor Red
    Write-Host ''
    throw 'Das Passwort wurde gesetzt, die Anmeldung schlaegt aber weiter fehl.'
}
