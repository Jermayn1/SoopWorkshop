#Requires -Version 5.1
<#
.SYNOPSIS
    Legt die Kategorie "OOP" mit der Aufgabe "Bankkonto" an — zwei Klassen, die
    voneinander abhängen.

.DESCRIPTION
    Die Aufgabe für den Mehrklassen-Fall: der Aufgaben-Vertrag fordert `Konto`
    UND `Kunde`, und jede Methode gehört zu einer bestimmten Klasse. Die
    JUnit-Datei benutzt beide Klassen gemeinsam.

    Bewusst getrennt von den übrigen Seed-Skripten, damit alle unabhängig
    voneinander laufen und keins die Daten des anderen anfasst.

    Mehrfach ausführbar: eine bereits vorhandene Kategorie gleichen Namens wird
    vorher gelöscht.

.EXAMPLE
    .\tests\manual\seed-oop.ps1
#>
[CmdletBinding()]
param(
    [string]$ApiBaseUrl = 'http://localhost:5120',
    [string]$AdminPassword
)

$ErrorActionPreference = 'Stop'

$categoryName = 'OOP'
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
    # seed-phase3.ps1: Get-Content hängt Provider-Eigenschaften an, die
    # ConvertTo-Json zu einem hundert Megabyte grossen Body ausrollt.
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

Write-Host "Lege die Kategorie '$categoryName' gegen $ApiBaseUrl an." -ForegroundColor Cyan

$script:sitzung = Connect-Admin -ApiBaseUrl $ApiBaseUrl -AdminPassword $AdminPassword -ScriptRoot $PSScriptRoot
Write-Host '  angemeldet'

# Nur die eigene Kategorie entfernen. Indexzugriff statt Pipeline - unter
# Windows PowerShell 5.1 kommt die Liste als EIN Objekt durch die Funktion und
# wird nicht aufgeblättert, wodurch ein Filter still jede Kategorie träfe.
$antwort = Invoke-Api -Method Get -Path '/api/admin/categories'

for ($i = 0; $i -lt $antwort.Count; $i++) {
    $old = $antwort[$i]
    if ($old.name -ne $categoryName) { continue }

    Invoke-Api -Method Delete -Path "/api/admin/categories/$($old.id)" | Out-Null
    Write-Host "  entferne vorherige Kategorie '$($old.name)' ($($old.id))"
}

$category = Invoke-Api -Method Post -Path '/api/admin/categories' -Body @{
    name  = $categoryName
    order = 92
}
Invoke-Api -Method Patch -Path "/api/admin/categories/$($category.id)/visibility" | Out-Null
Write-Host "Kategorie angelegt: $($category.id)"

$aufgabe = Invoke-Api -Method Post -Path '/api/admin/tasks' -Body @{
    taskCategoryId = $category.id
    title          = 'Bankkonto'
    description    = @'
Schreibe **zwei** Klassen, die zusammenarbeiten: `Kunde` und `Konto`.

### Kunde

Ein Kunde hat einen Namen. Der Name wird im Konstruktor gesetzt und ändert sich nicht mehr.

- `Kunde(String name)`
- `String getName()`

### Konto

Ein Konto gehört genau einem Kunden und kennt seinen Stand.

- `Konto(Kunde inhaber)` — der Stand beginnt bei `0`
- `Kunde getInhaber()`
- `double getStand()`
- `void einzahlen(double betrag)` — erhöht den Stand
- `boolean abheben(double betrag)` — nimmt den Betrag ab und gibt `true` zurück.
  **Reicht der Stand nicht**, bleibt er unverändert und die Methode gibt `false` zurück.

### Abgabe

Lade **beide Dateien zusammen** hoch: `Kunde.java` und `Konto.java`. Sie werden
gemeinsam übersetzt, du darfst also aus der einen Klasse die andere benutzen.

Ein Überziehen des Kontos gibt es nicht — das ist der Teil, den die Tests am
genauesten ansehen.
'@
    difficulty     = 1
    order          = 1
    evaluationMode = 1
    expectedTypes  = @(
        @{
            name    = 'Kunde'
            methods = @('public String getName()')
        },
        @{
            name    = 'Konto'
            methods = @(
                'public Kunde getInhaber()',
                'public double getStand()',
                'public void einzahlen(double betrag)',
                'public boolean abheben(double betrag)'
            )
        }
    )
    hints          = @(
        'Der Name des Kunden und der Inhaber des Kontos ändern sich nie — dafür ist final gedacht.',
        'Ein Feld ohne eigene Zuweisung startet bei 0. Du musst den Stand im Konstruktor nicht extra setzen.',
        'Bei abheben zuerst prüfen, dann abziehen. Wer erst abzieht und danach schaut, hat das Geld schon weg.',
        'Konto braucht kein eigenes getName — es fragt seinen Inhaber.'
    )
}

Invoke-Api -Method Put -Path "/api/admin/tasks/$($aufgabe.id)/unittests" -Body @{
    taskItemId = $aufgabe.id
    files      = @(
        @{
            fileName               = 'BankTest.java'
            content                = Get-JUnitFile -Name 'BankTest.java'
            order                  = 1
            # Sichtbar: die Aufgabe lebt davon, dass man das erwartete Verhalten
            # bei fehlender Deckung genau kennt. Wer den Test lesen darf, rät nicht.
            isVisibleToParticipant = $true
        }
    )
} | Out-Null

Invoke-Api -Method Patch -Path "/api/admin/tasks/$($aufgabe.id)/visibility" | Out-Null
Write-Host "Aufgabe (UnitTestOnly, zwei Klassen): $($aufgabe.id)"

Write-Host ''
Write-Host 'Fertig. Passende Abgaben liegen unter tests/manual/junit/loesungen/:' -ForegroundColor Green
Write-Host '  bank                              Musterlösung — alles grün'
Write-Host '  bank-ohne-deckungspruefung        Vertrag stimmt, kompiliert, aber abheben prüft die'
Write-Host '                                    Deckung nicht: genau ein JUnit-Test fällt durch'
Write-Host '  bank-methode-in-falscher-klasse   getStand steht in Kunde statt in Konto: die'
Write-Host '                                    Vertragsprüfung fällt durch, und die JUnit-Datei'
Write-Host '                                    übersetzt nicht mehr gegen die Abgabe'
Write-Host ''
Write-Host 'Immer BEIDE Dateien eines Ordners zusammen hochladen.' -ForegroundColor Yellow
Write-Host ''
