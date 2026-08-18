#Requires -Version 5.1
<#
.SYNOPSIS
    Fuehrt alle automatisierten Pruefungen des Projekts in einem Durchgang aus.

.DESCRIPTION
    Baut die Solution, laesst die Projekt-Tests laufen, baut das Frontend
    (darin steckt tsc -b, die Typpruefung gegen den erzeugten API-Vertrag),
    laesst die Frontend-Tests laufen und prueft den Linter.

    Am Ende steht eine Zusammenfassung, welcher Schritt gefallen ist. Das
    Skript bricht NICHT beim ersten Fehler ab: wer fuenf Pruefungen laufen
    laesst, will alle fuenf Ergebnisse sehen und nicht fuenfmal neu starten.

    Ersetzt die Einzelbefehle aus CLAUDE.md Paragraph 7.

.PARAMETER OhneDocker
    Laesst die Integrationstests aus. Sie brauchen Docker, weil sie gegen ein
    echtes PostgreSQL in einem Container laufen (Testcontainers).

.PARAMETER MitCoverage
    Erzeugt zusaetzlich Coverage-Berichte unter artifacts/coverage/.
    ReportGenerator liegt als lokales Werkzeug im Repository und wird beim ersten
    Lauf selbst wiederhergestellt - nichts zu installieren.

.EXAMPLE
    .\scripts\pruefe-alles.ps1

.EXAMPLE
    .\scripts\pruefe-alles.ps1 -OhneDocker

.EXAMPLE
    .\scripts\pruefe-alles.ps1 -MitCoverage
#>
[CmdletBinding()]
param(
    [switch]$OhneDocker,
    [switch]$MitCoverage
)

# Bewusst NICHT 'Stop': die einzelnen Schritte duerfen fehlschlagen, ohne das
# Skript zu beenden - sonst faende man immer nur den ersten Fehler.
$ErrorActionPreference = 'Continue'

$repoRoot = Split-Path -Parent $PSScriptRoot
$frontend = Join-Path $repoRoot 'src\SoopWorkshop.Frontend'
$coverageRoot = Join-Path $repoRoot 'artifacts\coverage'

$ergebnisse = [System.Collections.Generic.List[object]]::new()

function Invoke-Schritt {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Aktion
    )

    Write-Host ''
    Write-Host "── $Name " -ForegroundColor Cyan -NoNewline
    Write-Host ('─' * [Math]::Max(0, 60 - $Name.Length)) -ForegroundColor DarkGray

    $start = Get-Date
    & $Aktion
    $erfolg = $LASTEXITCODE -eq 0
    $dauer = (Get-Date) - $start

    $script:ergebnisse.Add([pscustomobject]@{
        Schritt = $Name
        Erfolg  = $erfolg
        Dauer   = '{0:N1}s' -f $dauer.TotalSeconds
    })
}

Write-Host ''
Write-Host 'SoopWorkshop — alle Pruefungen' -ForegroundColor White
if ($OhneDocker) {
    Write-Host 'Integrationstests werden ausgelassen (-OhneDocker).' -ForegroundColor Yellow
}

# Das Backend haelt seine DLLs. Ein Build bei laufendem Backend scheitert mit
# CS2012 - lieber vorher sagen als hinterher raten.
$laufend = Get-Process -Name 'SoopWorkshop.Backend.API' -ErrorAction SilentlyContinue
if ($laufend) {
    Write-Host ''
    Write-Host 'Das Backend laeuft und haelt seine DLLs — der Build wird scheitern.' -ForegroundColor Red
    Write-Host 'Erst .\scripts\stop-dev.ps1 aufrufen, dann noch einmal hier.' -ForegroundColor Red
    exit 1
}

Invoke-Schritt 'dotnet build' {
    dotnet build (Join-Path $repoRoot 'SoopWorkshop.slnx') --nologo -v quiet
}

Invoke-Schritt 'dotnet test' {
    $argumente = @((Join-Path $repoRoot 'SoopWorkshop.slnx'), '--nologo', '-v', 'quiet', '--no-build')

    if ($OhneDocker) {
        $argumente += @('--filter', 'Category!=Integration')
    }

    if ($MitCoverage) {
        $argumente += @(
            '--collect:XPlat Code Coverage',
            '--results-directory', (Join-Path $coverageRoot 'backend-raw')
        )
    }

    dotnet test @argumente
}

Invoke-Schritt 'npm run build' {
    npm --prefix $frontend run build
}

Invoke-Schritt 'npm run test' {
    if ($MitCoverage) {
        npm --prefix $frontend run test:coverage
    } else {
        npm --prefix $frontend test
    }
}

Invoke-Schritt 'npm run lint' {
    npm --prefix $frontend run lint
}

if ($MitCoverage) {
    Invoke-Schritt 'Coverage-Bericht' {
        # ReportGenerator liegt als lokales Werkzeug im Repository
        # (.config/dotnet-tools.json) - keine Installation auf der Maschine
        # noetig, aber einmalig wiederherstellen.
        dotnet tool restore | Out-Null

        dotnet reportgenerator `
            "-reports:$(Join-Path $coverageRoot 'backend-raw\**\coverage.cobertura.xml')" `
            "-targetdir:$(Join-Path $coverageRoot 'backend')" `
            '-reporttypes:Html;TextSummary' | Out-Null

        if ($LASTEXITCODE -eq 0) {
            $zusammenfassung = Join-Path $coverageRoot 'backend\Summary.txt'
            if (Test-Path $zusammenfassung) {
                Get-Content $zusammenfassung |
                    Select-String -Pattern 'Line coverage|Branch coverage|Method coverage' |
                    ForEach-Object { Write-Host "  $_" }
            }
        }
    }
}

Write-Host ''
Write-Host ('─' * 64) -ForegroundColor DarkGray
Write-Host 'Zusammenfassung' -ForegroundColor White
Write-Host ''

foreach ($ergebnis in $ergebnisse) {
    $zeichen = if ($ergebnis.Erfolg) { 'OK  ' } else { 'FEHL' }
    $farbe = if ($ergebnis.Erfolg) { 'Green' } else { 'Red' }

    Write-Host ('  {0}  {1,-22} {2,6}' -f $zeichen, $ergebnis.Schritt, $ergebnis.Dauer) -ForegroundColor $farbe
}

$gefallen = @($ergebnisse | Where-Object { -not $_.Erfolg })

Write-Host ''
if ($gefallen.Count -eq 0) {
    Write-Host 'Alles gruen.' -ForegroundColor Green
    if ($MitCoverage) {
        Write-Host "Coverage: $coverageRoot" -ForegroundColor DarkGray
    }
    exit 0
}

Write-Host "$($gefallen.Count) Schritt(e) gefallen: $($gefallen.Schritt -join ', ')" -ForegroundColor Red
exit 1
