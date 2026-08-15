<#
.SYNOPSIS
    Legt die Beispielaufgaben für den manuellen Durchlauf der Bewertungs-Engine an.

.DESCRIPTION
    Erzeugt eine Kategorie mit drei Aufgaben, die zusammen alle drei
    Auswertungsmodi abdecken, und schaltet sie sichtbar:

      1. "Hallo Soop (Konsole)"     ConsoleOnly   - klassische Konsolen-Testfälle
      2. "Hallo Soop (Unit-Test)"   UnitTestOnly  - dieselbe Aufgabe, aber über
                                                    JUnit geprüft. Der Teilnehmer
                                                    schreibt nur eine main.
      3. "Rechner"                  Both          - eigene Methode plus Ausgabe

    Die JUnit-Dateien kommen aus tests/manual/junit/tests/, die passenden
    Abgaben liegen unter tests/manual/junit/loesungen/.

    Mehrfach ausführbar: eine bereits vorhandene Kategorie gleichen Namens wird
    vorher gelöscht, damit keine Karteileichen entstehen.

.PARAMETER ApiBaseUrl
    Basis-URL der laufenden API. Standard: http://localhost:5120

.EXAMPLE
    .\tests\manual\seed-phase3.ps1
#>

[CmdletBinding()]
param(
    [string]$ApiBaseUrl = 'http://localhost:5120'
)

$ErrorActionPreference = 'Stop'

$categoryName = 'Phase 3 - Beispiele'
$junitRoot = Join-Path $PSScriptRoot 'junit'

function Invoke-Api {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        $Body
    )

    $uri = "$ApiBaseUrl$Path"

    if ($null -eq $Body) {
        return Invoke-RestMethod -Method $Method -Uri $uri
    }

    # Body als UTF-8-Bytes statt als String: Windows PowerShell 5.1 kodiert
    # Strings sonst in der Codepage des Systems und zerlegt damit Umlaute.
    $bytes = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 8))

    return Invoke-RestMethod -Method $Method -Uri $uri -Body $bytes -ContentType 'application/json; charset=utf-8'
}

function Get-JUnitFile {
    param([Parameter(Mandatory)][string]$Name)

    $path = Join-Path (Join-Path $junitRoot 'tests') $Name
    if (-not (Test-Path $path)) {
        throw "JUnit-Vorlage '$path' fehlt."
    }

    # Bewusst File::ReadAllText statt Get-Content: Get-Content haengt an jeden
    # zurueckgegebenen String Provider-Eigenschaften (PSPath, PSDrive, ...).
    # ConvertTo-Json rollt dieses Objekt dann rekursiv aus und macht aus 1,8 KB
    # Datei ueber 100 MB JSON - der Server lehnt das als "Request body too large"
    # ab, und der Fehler zeigt auf eine voellig unschuldige Stelle.
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

Write-Host "Lege Beispieldaten gegen $ApiBaseUrl an." -ForegroundColor Cyan

# Aufraeumen, damit ein zweiter Lauf nicht die Sidebar zumuellt. Es wird
# ausschliesslich auf den exakten Namen dieses Skripts gefiltert - fremde
# Kategorien bleiben unangetastet. Der Name steht deshalb in der Ausgabe: ein
# Loeschvorgang, den man nicht nachlesen kann, ist einer zu viel.
#
# Bewusst ueber den Index statt mit foreach oder Where-Object: unter Windows
# PowerShell 5.1 kommt die Liste durch die Funktion hindurch als EIN Objekt an
# und wird in der Pipeline nicht aufgeblaettert. '$_.name' liefert dann alle
# Namen auf einmal, '-eq' filtert das Array statt zu vergleichen, und das
# nicht leere Ergebnis gilt als wahr - womit hier jede Kategorie geloescht
# wuerde. Der Indexzugriff funktioniert fuer eine Liste wie fuer ein einzelnes
# Objekt gleichermassen.
$antwort = Invoke-Api -Method Get -Path '/api/admin/categories'

for ($i = 0; $i -lt $antwort.Count; $i++) {
    $old = $antwort[$i]

    if ($old.name -ne $categoryName) {
        continue
    }

    Invoke-Api -Method Delete -Path "/api/admin/categories/$($old.id)" | Out-Null
    Write-Host "  entferne vorherige Kategorie '$($old.name)' ($($old.id))"
}

$category = Invoke-Api -Method Post -Path '/api/admin/categories' -Body @{
    name  = $categoryName
    order = 90
}
Invoke-Api -Method Patch -Path "/api/admin/categories/$($category.id)/visibility" | Out-Null
Write-Host "Kategorie angelegt: $($category.id)"

# ── 1. ConsoleOnly ──────────────────────────────────────────────
$konsole = Invoke-Api -Method Post -Path '/api/admin/tasks' -Body @{
    taskCategoryId    = $category.id
    title             = 'Hallo Soop (Konsole)'
    description       = 'Schreibe die Klasse Main und gib genau "Hallo Soop" auf der Konsole aus.'
    difficulty        = 0
    order             = 1
    evaluationMode    = 0
    expectedClassName = 'Main'
    expectedMethods   = @('public static void main(String[] args)')
    hints             = @('System.out.println gibt eine Zeile aus.')
}

Invoke-Api -Method Post -Path "/api/admin/tasks/$($konsole.id)/tests" -Body @{
    taskItemId     = $konsole.id
    input          = ''
    expectedOutput = 'Hallo Soop'
    description    = 'Das Programm gibt "Hallo Soop" aus'
    order           = 1
} | Out-Null

Invoke-Api -Method Patch -Path "/api/admin/tasks/$($konsole.id)/visibility" | Out-Null
Write-Host "Aufgabe 1 (ConsoleOnly): $($konsole.id)"

# ── 2. UnitTestOnly, ohne dass der Teilnehmer eine Methode schreibt ──
$unitOnly = Invoke-Api -Method Post -Path '/api/admin/tasks' -Body @{
    taskCategoryId    = $category.id
    title             = 'Hallo Soop (Unit-Test)'
    description       = 'Dieselbe Aufgabe wie zuvor, geprueft ueber einen Unit-Test statt ueber Konsolen-Testfaelle.'
    difficulty        = 0
    order             = 2
    evaluationMode    = 1
    expectedClassName = 'Main'
    expectedMethods   = @('public static void main(String[] args)')
    hints             = @('Der Test ruft deine main auf und liest mit, was du ausgibst.')
}

Invoke-Api -Method Put -Path "/api/admin/tasks/$($unitOnly.id)/unittests" -Body @{
    taskItemId = $unitOnly.id
    files      = @(
        @{
            fileName               = 'HalloSoopTest.java'
            content                = Get-JUnitFile -Name 'HalloSoopTest.java'
            order                  = 1
            isVisibleToParticipant = $false
        }
    )
} | Out-Null

Invoke-Api -Method Patch -Path "/api/admin/tasks/$($unitOnly.id)/visibility" | Out-Null
Write-Host "Aufgabe 2 (UnitTestOnly): $($unitOnly.id)"

# ── 3. Both ─────────────────────────────────────────────────────
$both = Invoke-Api -Method Post -Path '/api/admin/tasks' -Body @{
    taskCategoryId    = $category.id
    title             = 'Rechner'
    description       = 'Schreibe die Klasse Main. Lies zwei ganze Zahlen ein und gib ihre Summe aus. Die Addition gehoert in eine eigene Methode addiere.'
    difficulty        = 1
    order             = 3
    evaluationMode    = 2
    expectedClassName = 'Main'
    expectedMethods   = @(
        'public static int addiere(int ersteZahl, int zweiteZahl)',
        'public static void main(String[] args)'
    )
    hints             = @('Scanner liest Zahlen mit nextInt().', 'Die Methode muss static sein, damit der Test sie ohne Objekt aufrufen kann.')
}

Invoke-Api -Method Post -Path "/api/admin/tasks/$($both.id)/tests" -Body @{
    taskItemId     = $both.id
    input          = "3`n4`n"
    expectedOutput = '7'
    description    = '3 und 4 ergeben 7'
    order          = 1
} | Out-Null

Invoke-Api -Method Post -Path "/api/admin/tasks/$($both.id)/tests" -Body @{
    taskItemId     = $both.id
    input          = "10`n-4`n"
    expectedOutput = '6'
    description    = '10 und -4 ergeben 6'
    order          = 2
} | Out-Null

Invoke-Api -Method Put -Path "/api/admin/tasks/$($both.id)/unittests" -Body @{
    taskItemId = $both.id
    files      = @(
        @{
            fileName               = 'RechnerTest.java'
            content                = Get-JUnitFile -Name 'RechnerTest.java'
            order                  = 1
            isVisibleToParticipant = $false
        }
    )
} | Out-Null

Invoke-Api -Method Patch -Path "/api/admin/tasks/$($both.id)/visibility" | Out-Null
Write-Host "Aufgabe 3 (Both): $($both.id)"

Write-Host ''
Write-Host 'Fertig. Passende Abgaben liegen unter tests/manual/junit/loesungen/:' -ForegroundColor Green
Write-Host '  hallo-soop                        Musterloesung fuer Aufgabe 1 und 2'
Write-Host '  hallo-soop-tippfehler             faellt in Aufgabe 2 durch, zeigt erwartet gegen erhalten'
Write-Host '  rechner                           Musterloesung fuer Aufgabe 3'
Write-Host '  rechner-falscher-klassenname      Rechner.java statt Main.java - kompiliert, verletzt aber die Vorgabe'
Write-Host '  rechner-falscher-methodenname     Testdatei kompiliert nicht - nennt die erwartete Signatur'
Write-Host '  rechner-falscher-rueckgabewert    kompiliert, faellt inhaltlich durch'
Write-Host '  rechner-umlaute                   prueft die UTF-8-Kette bis in die Anzeige'
Write-Host '  rechner-system-exit               beendet die JVM - muss verstaendlich gemeldet werden'
