#Requires -Version 5.1
<#
.SYNOPSIS
    Legt die Kategorie "Schleifen" mit der Aufgabe "Sternchen-Pyramide" an.

.DESCRIPTION
    Eine eigenständige Beispielaufgabe im Modus UnitTestOnly: geprüft wird eine
    Methode mit Rückgabewert, nicht die Konsolenausgabe.

    Bewusst getrennt von seed-phase3.ps1, damit beide Skripte unabhängig
    voneinander laufen können und keins die Daten des anderen anfasst.

    Mehrfach ausführbar: eine bereits vorhandene Kategorie gleichen Namens wird
    vorher gelöscht.

.EXAMPLE
    .\tests\manual\seed-pyramide.ps1
#>
[CmdletBinding()]
param(
    [string]$ApiBaseUrl = 'http://localhost:5120',
    [string]$AdminPassword
)

$ErrorActionPreference = 'Stop'

$categoryName = 'Schleifen'
$junitRoot = Join-Path $PSScriptRoot 'junit'

. (Join-Path $PSScriptRoot 'admin-anmeldung.ps1')

function Invoke-Api {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        $Body
    )

    $uri = "$ApiBaseUrl$Path"

    if ($null -eq $Body) {
        return Invoke-RestMethod -Method $Method -Uri $uri -WebSession $script:sitzung
    }

    # Body als UTF-8-Bytes statt als String: Windows PowerShell 5.1 kodiert
    # Strings sonst in der Codepage des Systems und zerlegt damit Umlaute.
    $bytes = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 8))

    return Invoke-RestMethod -Method $Method -Uri $uri -WebSession $script:sitzung -Body $bytes -ContentType 'application/json; charset=utf-8'
}

function Get-JUnitFile {
    param([Parameter(Mandatory)][string]$Name)

    $path = Join-Path (Join-Path $junitRoot 'tests') $Name
    if (-not (Test-Path $path)) {
        throw "JUnit-Vorlage '$path' fehlt."
    }

    # Bewusst File::ReadAllText statt Get-Content - siehe Begründung in
    # seed-phase3.ps1: Get-Content haengt Provider-Eigenschaften an, die
    # ConvertTo-Json zu einem hundert Megabyte grossen Body ausrollt.
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

Write-Host "Lege die Kategorie '$categoryName' gegen $ApiBaseUrl an." -ForegroundColor Cyan

$script:sitzung = Connect-Admin -ApiBaseUrl $ApiBaseUrl -AdminPassword $AdminPassword -ScriptRoot $PSScriptRoot
Write-Host '  angemeldet'

# Nur die eigene Kategorie entfernen. Indexzugriff statt Pipeline - unter
# Windows PowerShell 5.1 kommt die Liste als EIN Objekt durch die Funktion und
# wird nicht aufgeblaettert, wodurch ein Filter still jede Kategorie traefe.
$antwort = Invoke-Api -Method Get -Path '/api/admin/categories'

for ($i = 0; $i -lt $antwort.Count; $i++) {
    $old = $antwort[$i]
    if ($old.name -ne $categoryName) { continue }

    Invoke-Api -Method Delete -Path "/api/admin/categories/$($old.id)" | Out-Null
    Write-Host "  entferne vorherige Kategorie '$($old.name)' ($($old.id))"
}

$category = Invoke-Api -Method Post -Path '/api/admin/categories' -Body @{
    name     = $categoryName
    order    = 91
    iconName = 'Repeat'
}
Invoke-Api -Method Patch -Path "/api/admin/categories/$($category.id)/visibility" | Out-Null
Write-Host "Kategorie angelegt: $($category.id)"

$aufgabe = Invoke-Api -Method Post -Path '/api/admin/tasks' -Body @{
    taskCategoryId    = $category.id
    title             = 'Sternchen-Pyramide'
    description       = @'
Schreibe die Klasse `Main` mit der Methode `zeichnePyramide`. Sie bekommt eine Höhe und **gibt die Pyramide als Text zurück** — sie gibt sie nicht selbst aus.

Bei Höhe 3 sieht das Ergebnis so aus:

```
  *
 ***
*****
```

Die Regeln:

- Zeile `i` hat `2 * i - 1` Sternchen, die Spitze steht mittig.
- Die Zeilen werden mit `\n` verbunden. **Nach der letzten Zeile kommt kein Zeilenumbruch.**
- Am Zeilenende stehen keine Leerzeichen.

`main` darf die Pyramide anschließend ausgeben — geprüft wird aber die Methode.
'@
    difficulty        = 1
    order             = 1
    evaluationMode    = 1
    expectedTypes     = @(
        @{
            name    = 'Main'
            methods = @(
                'public static String zeichnePyramide(int hoehe)',
                'public static void main(String[] args)'
            )
        }
    )
    hints             = @(
        'Zwei Schleifen ineinander: die äußere zählt die Zeilen, die innere setzt die Zeichen.',
        'In Zeile i stehen erst (höhe - i) Leerzeichen, danach (2 * i - 1) Sternchen.',
        'Ein StringBuilder ist bequemer als Text immer wieder mit + zusammenzusetzen.',
        'Den Zeilenumbruch hängst du am besten vor der nächsten Zeile an — dann bleibt die letzte ohne.'
    )
}

Invoke-Api -Method Put -Path "/api/admin/tasks/$($aufgabe.id)/unittests" -Body @{
    taskItemId = $aufgabe.id
    files      = @(
        @{
            fileName               = 'PyramideTest.java'
            content                = Get-JUnitFile -Name 'PyramideTest.java'
            order                  = 1
            # Sichtbar: die Aufgabe lebt davon, dass man die erwartete Form
            # genau kennt. Wer den Test lesen darf, raet nicht.
            isVisibleToParticipant = $true
        }
    )
} | Out-Null

Invoke-Api -Method Patch -Path "/api/admin/tasks/$($aufgabe.id)/visibility" | Out-Null
Write-Host "Aufgabe (UnitTestOnly): $($aufgabe.id)"

Write-Host ''
Write-Host 'Fertig. Passende Abgaben liegen unter tests/manual/junit/loesungen/:' -ForegroundColor Green
Write-Host '  pyramide                 Musterlösung, besteht alle vier Teilprüfungen'
Write-Host '  pyramide-linksbuendig    Sternchen stimmen, Einrückung fehlt - fällt bei genau einer durch'
Write-Host ''
