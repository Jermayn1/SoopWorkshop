#Requires -Version 5.1
<#
.SYNOPSIS
    Startet die Entwicklungsumgebung: PostgreSQL und Backend-API.

.DESCRIPTION
    Prueft den PostgreSQL-Container und startet ihn bei Bedarf, baut die Solution
    und startet Backend und Frontend in je einem eigenen Fenster.

    Das Frontend ist seit Phase 4 React + Vite (src\SoopWorkshop.Frontend) und
    laeuft auf Port 5173 — derselbe Port steht im Backend unter
    Cors:AllowedOrigins. Fehlen die npm-Pakete, werden sie einmalig installiert.

    Zum Beenden die Fenster schliessen oder .\scripts\stop-dev.ps1 aufrufen.

.PARAMETER SkipBuild
    Ueberspringt den Build. Nuetzlich, wenn gerade erst gebaut wurde.

.PARAMETER NoDatabase
    Laesst den PostgreSQL-Container unberuehrt.

.EXAMPLE
    .\scripts\start-dev.ps1

.EXAMPLE
    .\scripts\start-dev.ps1 -SkipBuild
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$NoDatabase,
    [string]$DbContainer = 'soopworkshop-db'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$backendUrl = 'http://localhost:5120'
$frontendUrl = 'http://localhost:5173'
$frontendPath = Join-Path $repoRoot 'src\SoopWorkshop.Frontend'

function Write-Step {
    param([string]$Text)
    Write-Host ''
    Write-Host "==> $Text" -ForegroundColor Cyan
}

# Prueft, ob auf einem Port bereits jemand horcht. Schneller als Test-NetConnection.
function Test-Port {
    param([int]$Port)

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $connect = $client.BeginConnect('127.0.0.1', $Port, $null, $null)
        $ok = $connect.AsyncWaitHandle.WaitOne(300)
        if ($ok) { $client.EndConnect($connect) }
        return $ok
    }
    catch {
        return $false
    }
    finally {
        $client.Close()
    }
}

function Wait-ForPort {
    param([int]$Port, [string]$Name, [int]$TimeoutSeconds = 60)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Port -Port $Port) {
            Write-Host "    $Name ist bereit (Port $Port)." -ForegroundColor Green
            return $true
        }
        Start-Sleep -Milliseconds 500
    }

    Write-Host "    $Name antwortet nach $TimeoutSeconds s nicht auf Port $Port." -ForegroundColor Yellow
    return $false
}

# Startet ein Projekt in einem eigenen Fenster, damit die Logs getrennt lesbar bleiben.
function Start-DevService {
    param([string]$Title, [string]$Project)

    $command = "`$Host.UI.RawUI.WindowTitle = '$Title'; dotnet run --project '$Project' --launch-profile http"
    Start-Process -FilePath 'powershell' -ArgumentList '-NoExit', '-Command', $command | Out-Null
    Write-Host "    $Title gestartet." -ForegroundColor Green
}

# ── 1. Datenbank ────────────────────────────────────────────────
if (-not $NoDatabase) {
    Write-Step 'PostgreSQL pruefen'

    $dockerAvailable = $null -ne (Get-Command docker -ErrorAction SilentlyContinue)
    if (-not $dockerAvailable) {
        Write-Host '    Docker nicht gefunden. Laeuft PostgreSQL anders, nutze -NoDatabase.' -ForegroundColor Yellow
    }
    else {
        $running = docker ps --filter "name=$DbContainer" --filter 'status=running' --format '{{.Names}}'
        if ($running -eq $DbContainer) {
            Write-Host "    Container '$DbContainer' laeuft bereits." -ForegroundColor Green
        }
        else {
            $exists = docker ps -a --filter "name=$DbContainer" --format '{{.Names}}'
            if ($exists -eq $DbContainer) {
                Write-Host "    Container '$DbContainer' wird gestartet ..."
                docker start $DbContainer | Out-Null
                Wait-ForPort -Port 5432 -Name 'PostgreSQL' -TimeoutSeconds 30 | Out-Null
            }
            else {
                Write-Host "    Container '$DbContainer' existiert nicht." -ForegroundColor Yellow
                Write-Host '    Ab Phase 7 uebernimmt das docker-compose.yml diesen Schritt.' -ForegroundColor Yellow
            }
        }
    }
}

# ── 2. Build ────────────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Step 'Solution bauen'
    dotnet build SoopWorkshop.slnx --nologo
    if ($LASTEXITCODE -ne 0) {
        throw 'Build fehlgeschlagen. Backend und Frontend wurden nicht gestartet.'
    }
}

# ── 3. Dienste starten ──────────────────────────────────────────
Write-Step 'Backend starten'

if (Test-Port -Port 5120) {
    Write-Host "    Port 5120 ist belegt - Backend laeuft vermutlich schon." -ForegroundColor Yellow
}
else {
    Start-DevService -Title 'SoopWorkshop API' -Project 'src\SoopWorkshop.Backend.API'
    Wait-ForPort -Port 5120 -Name 'Backend' | Out-Null
}

Write-Step 'Frontend starten'

if (Test-Port -Port 5173) {
    Write-Host "    Port 5173 ist belegt - Frontend laeuft vermutlich schon." -ForegroundColor Yellow
}
elseif (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host '    npm nicht gefunden. Node.js installieren, dann erneut starten.' -ForegroundColor Yellow
}
else {
    # Beim ersten Start fehlen die Pakete. Ohne diesen Zweig bricht Vite mit
    # einer Meldung ab, die nach einem kaputten Projekt aussieht.
    if (-not (Test-Path (Join-Path $frontendPath 'node_modules'))) {
        Write-Host '    node_modules fehlt - npm install laeuft einmalig ...'
        npm --prefix $frontendPath install
        if ($LASTEXITCODE -ne 0) { throw 'npm install fehlgeschlagen.' }
    }

    $command = "`$Host.UI.RawUI.WindowTitle = 'SoopWorkshop Frontend'; npm --prefix '$frontendPath' run dev"
    Start-Process -FilePath 'powershell' -ArgumentList '-NoExit', '-Command', $command | Out-Null
    Write-Host '    SoopWorkshop Frontend gestartet.' -ForegroundColor Green
    Wait-ForPort -Port 5173 -Name 'Frontend' | Out-Null
}

# ── 4. Uebersicht ───────────────────────────────────────────────
Write-Step 'Bereit'
Write-Host "    Frontend   $frontendUrl"
Write-Host "    API        $backendUrl"
Write-Host "    API-Doku   $backendUrl/scalar"
Write-Host ''
Write-Host '    Beenden: die Fenster schliessen oder .\scripts\stop-dev.ps1' -ForegroundColor DarkGray
Write-Host ''
