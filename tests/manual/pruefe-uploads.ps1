#Requires -Version 5.1
<#
.SYNOPSIS
    Prueft die serverseitige Upload-Validierung und den Status-Endpunkt.

.DESCRIPTION
    Deckt die Faelle ab, die sich im Browser nicht ausloesen lassen, weil das
    Frontend vorher blockt: Dateinamen mit Pfadanteilen, doppelte Namen,
    unbekannte Aufgaben-ID. Die dafuer noetigen Dateien erzeugt das Skript im
    Speicher - im Repository liegen deshalb keine kaputten Beispieldateien.

    Voraussetzung: das Backend laeuft (.\scripts\start-dev.ps1) und es gibt
    mindestens eine sichtbare Aufgabe.

.PARAMETER TaskItemId
    Aufgabe, gegen die eingereicht wird. Ohne Angabe nimmt das Skript die erste
    sichtbare Aufgabe aus der API.

.EXAMPLE
    .\tests\manual\pruefe-uploads.ps1
#>
[CmdletBinding()]
param(
    [string]$ApiBaseUrl = "http://localhost:5120",
    [string]$TaskItemId = ""
)

Add-Type -AssemblyName System.Net.Http

$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(30)

# Ohne vorgegebene Aufgabe die erste sichtbare aus der API nehmen, damit das
# Skript ohne maschinenspezifische GUID auskommt.
if ([string]::IsNullOrWhiteSpace($TaskItemId)) {
    $antwort = $client.GetAsync("$ApiBaseUrl/api/categories").Result

    if (-not $antwort.IsSuccessStatusCode) {
        throw "Die API unter $ApiBaseUrl antwortet nicht. Laeuft das Backend?"
    }

    $kategorien = $antwort.Content.ReadAsStringAsync().Result | ConvertFrom-Json
    $aufgabe = $kategorien | ForEach-Object { $_.tasks } | Select-Object -First 1

    if ($null -eq $aufgabe) {
        throw 'Keine sichtbare Aufgabe gefunden. Zuerst ueber /scalar eine anlegen und sichtbar schalten.'
    }

    $TaskItemId = $aufgabe.id
    Write-Host "Aufgabe: $($aufgabe.title) ($TaskItemId)" -ForegroundColor DarkGray
}

$gueltigerInhalt = 'public class Main { public static void main(String[] a) { System.out.println("Hallo SOOP Workshop!"); } }'

function Send-Upload {
    param(
        [string]$TaskId,
        [array]$Dateien   # je @{ Name = "Main.java"; Inhalt = "..."; Groesse = 0 }
    )

    $content = New-Object System.Net.Http.MultipartFormDataContent
    $content.Add((New-Object System.Net.Http.StringContent($TaskId)), "taskItemId")

    foreach ($datei in $Dateien) {
        if ($datei.ContainsKey("Groesse")) {
            $bytes = New-Object byte[] $datei.Groesse
        } else {
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($datei.Inhalt)
        }

        $teil = New-Object System.Net.Http.ByteArrayContent(, $bytes)
        $content.Add($teil, "files", $datei.Name)
    }

    $antwort = $client.PostAsync("$ApiBaseUrl/api/submissions", $content).Result
    $koerper = $antwort.Content.ReadAsStringAsync().Result

    return @{ Code = [int]$antwort.StatusCode; Koerper = $koerper }
}

function Test-Fall {
    param(
        [string]$Name,
        [int]$ErwarteterCode,
        [string]$ErwarteterText,
        [scriptblock]$Aufruf
    )

    $ergebnis = & $Aufruf
    $codePasst = $ergebnis.Code -eq $ErwarteterCode
    $textPasst = [string]::IsNullOrEmpty($ErwarteterText) -or $ergebnis.Koerper -like "*$ErwarteterText*"

    if ($codePasst -and $textPasst) {
        Write-Host ("  OK    {0}" -f $Name) -ForegroundColor Green
        Write-Host ("        {0} {1}" -f $ergebnis.Code, $ergebnis.Koerper.Trim()) -ForegroundColor DarkGray
    } else {
        Write-Host ("  FEHL  {0}" -f $Name) -ForegroundColor Red
        Write-Host ("        erwartet: {0} mit '{1}'" -f $ErwarteterCode, $ErwarteterText) -ForegroundColor Red
        Write-Host ("        bekommen: {0} {1}" -f $ergebnis.Code, $ergebnis.Koerper.Trim()) -ForegroundColor Red
    }

    return $ergebnis
}

# Die Erwartungen unten sind bewusst auf umlautfreie Teilstuecke verkuerzt.
# Die API antwortet inzwischen mit echten Umlauten ("gueltiger Dateiname" heisst
# dort "g-u-umlaut-ltiger"), diese Datei hat aber keine BOM - und Windows
# PowerShell 5.1 laese einen Umlaut darin als ANSI. Der Vergleich traefe dann
# stillschweigend nie zu, und der Test meldete einen Fehler, den es nicht gibt.
# Die gekuerzten Teilstuecke sind weiterhin eindeutig; der volle Wortlaut steht
# in SubmissionUploadValidator.cs.
Write-Host ""
Write-Host "Upload-Validierung gegen $ApiBaseUrl" -ForegroundColor Cyan
Write-Host ""

Test-Fall "Falsche Endung (.png)" 400 "keine .java-Datei" {
    Send-Upload $TaskItemId @(@{ Name = "Bild.png"; Inhalt = $gueltigerInhalt })
} | Out-Null

Test-Fall "Dateiname mit Pfadanteil (..\..\)" 400 "Dateiname" {
    Send-Upload $TaskItemId @(@{ Name = "..\..\evil.java"; Inhalt = $gueltigerInhalt })
} | Out-Null

Test-Fall "Dateiname mit Unterordner" 400 "Dateiname" {
    Send-Upload $TaskItemId @(@{ Name = "unterordner/Main.java"; Inhalt = $gueltigerInhalt })
} | Out-Null

Test-Fall "Zweimal derselbe Dateiname" 400 "mehrfach" {
    Send-Upload $TaskItemId @(
        @{ Name = "Main.java"; Inhalt = $gueltigerInhalt },
        @{ Name = "Main.java"; Inhalt = $gueltigerInhalt }
    )
} | Out-Null

Test-Fall "Leere Datei" 400 "ist leer" {
    Send-Upload $TaskItemId @(@{ Name = "Leer.java"; Inhalt = "" })
} | Out-Null

Test-Fall "Datei groesser als 1 MB" 400 "1024 KB" {
    Send-Upload $TaskItemId @(@{ Name = "Gross.java"; Groesse = 1048577 })
} | Out-Null

Test-Fall "Elf Dateien" 400 "10 Dateien" {
    $dateien = 1..11 | ForEach-Object { @{ Name = "Datei$_.java"; Inhalt = "public class Datei$_ {}" } }
    Send-Upload $TaskItemId $dateien
} | Out-Null

Test-Fall "Unbekannte Aufgaben-ID" 400 "Aufgabe nicht gefunden" {
    Send-Upload ([Guid]::NewGuid().ToString()) @(@{ Name = "Main.java"; Inhalt = $gueltigerInhalt })
} | Out-Null

Write-Host ""
Write-Host "Status-Endpunkt" -ForegroundColor Cyan
Write-Host ""

$unbekannt = [Guid]::NewGuid().ToString()
$antwort = $client.GetAsync("$ApiBaseUrl/api/submissions/$unbekannt/status").Result

if ([int]$antwort.StatusCode -eq 404) {
    Write-Host "  OK    Unbekannte Abgabe liefert 404" -ForegroundColor Green
} else {
    Write-Host ("  FEHL  Unbekannte Abgabe liefert {0} statt 404" -f [int]$antwort.StatusCode) -ForegroundColor Red
}

Write-Host ""
Write-Host "Gueltige Abgabe (erzeugt eine echte Auswertung)" -ForegroundColor Cyan
Write-Host ""

$gueltig = Test-Fall "Eine gueltige .java-Datei" 200 "" {
    Send-Upload $TaskItemId @(@{ Name = "Main.java"; Inhalt = $gueltigerInhalt })
}

if ($gueltig.Code -eq 200) {
    $abgabe = $gueltig.Koerper | ConvertFrom-Json
    Write-Host ""
    Write-Host "  Status verfolgen:" -ForegroundColor Cyan
    Write-Host ("    $ApiBaseUrl/api/submissions/{0}/status" -f $abgabe.id)
    Write-Host ("    http://localhost:5072/result/{0}" -f $abgabe.id)
}

Write-Host ""
$client.Dispose()
