#Requires -Version 5.1
<#
.SYNOPSIS
    Legt Themenbereich 1 des Aufgabenpools an: Variablen, Rechnen, Strings, Scanner.

.DESCRIPTION
    Elf Aufgaben aus "Aufgabenpool: SOOP-Workshop", Themenbereich 1.

    Grundsaetze, die fuer alle Aufgaben dieser Reihe gelten:

      - Kein fertiger Loesungscode in der Aufgabenstellung. Die Beschreibung
        nennt die Bausteine und den Aufbau, setzt sie aber nicht zusammen -
        vorgemacht wird einmal im Workshop.
      - Jede Aufgabe traegt einen eigenen, sprechenden Klassennamen. Kein "Main".
      - Ab Aufgabe 2 waehlt der Teilnehmer den Datentyp selbst; die Aufgabe sagt
        nur noch, was gespeichert werden soll.
      - Vorgegebene Programmausgaben sind UMLAUTFREI. Der CharacterSetChecker
        prueft auch String-Literale, sonst kostet die Aufgabenstellung selbst
        Punkte in der Kategorie Clean Code.
      - Aufgaben ohne Eingabe werden ueber JUnit geprueft, Aufgaben mit Scanner
        ueber Konsolen-Testfaelle (CLAUDE.md §5.7).

    Mehrfach ausfuehrbar: eine bereits vorhandene Kategorie gleichen Namens wird
    vorher geloescht.

.EXAMPLE
    .\tests\manual\seed-themenbereich1.ps1
#>
[CmdletBinding()]
param(
    [string]$ApiBaseUrl = 'http://localhost:5120',
    [string]$AdminPassword
)

$ErrorActionPreference = 'Stop'

$categoryName = 'Variablen & Datentypen'
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
    if (-not (Test-Path $path)) { throw "JUnit-Vorlage '$path' fehlt." }

    # Bewusst File::ReadAllText statt Get-Content - Get-Content haengt
    # Provider-Eigenschaften an, die ConvertTo-Json rekursiv ausrollt.
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

# Legt eine Aufgabe an, haengt ihre Pruefungen dran und schaltet sie sichtbar.
function Add-Aufgabe {
    param(
        [Parameter(Mandatory)][hashtable]$Aufgabe,
        [string]$JUnitDatei,
        [array]$Testfaelle
    )

    $t = Invoke-Api -Method Post -Path '/api/admin/tasks' -Body $Aufgabe

    if ($JUnitDatei) {
        Invoke-Api -Method Put -Path "/api/admin/tasks/$($t.id)/unittests" -Body @{
            taskItemId = $t.id
            files      = @(
                @{
                    fileName = $JUnitDatei
                    content  = Get-JUnitFile -Name $JUnitDatei
                    order    = 1
                    # Verborgen: die Vorlagen arbeiten mit dem Compiler-API des
                    # JDK und wuerden einen Einsteiger mehr verwirren als leiten.
                    isVisibleToParticipant = $false
                }
            )
        } | Out-Null
    }

    if ($Testfaelle) {
        Invoke-Api -Method Put -Path "/api/admin/tasks/$($t.id)/tests" -Body @{
            taskItemId = $t.id
            tests      = $Testfaelle
        } | Out-Null
    }

    Invoke-Api -Method Patch -Path "/api/admin/tasks/$($t.id)/visibility" | Out-Null
    Write-Host ("  {0,-3} {1,-28} {2}" -f $Aufgabe.order, $Aufgabe.title, $t.id)
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
    order    = 1
    iconName = 'Variable'
}
Invoke-Api -Method Patch -Path "/api/admin/categories/$($category.id)/visibility" | Out-Null
Write-Host "Kategorie angelegt: $($category.id)"
Write-Host ''
# ---------------------------------------------------------------- 1
Add-Aufgabe -JUnitDatei 'SteckbriefTest.java' -Aufgabe @{
    taskCategoryId = $category.id
    title          = 'Deine erste Variable'
    difficulty     = 0
    order          = 1
    evaluationMode = 1
    expectedTypes  = @(@{ name = 'Steckbrief'; methods = @('public static void main(String[] args)') })
    description    = @'
Der Einstieg. Dein Programm rechnet nichts und gibt nichts aus — es geht darum, dass du dich in der IDE zurechtfindest und eine Variable sauber anlegst.

**Deine Aufgabe**

Lege die Klasse `Steckbrief` an und speichere darin das Alter einer Person: eine Variable mit dem Namen `age`, vom Typ `int`, mit dem Wert `25`.

### Grundlagen: Was ist eine Variable?

Eine Variable ist ein benannter Platz im Speicher. Drei Dinge gehören dazu:

1. **Datentyp** — was darf hineingelegt werden?
2. **Name** — wie sprichst du den Platz an?
3. **Wert** — was liegt gerade drin?

Aufgebaut ist eine Deklaration immer nach demselben Muster:

```
datentyp name = wert;
```

Der Wert darf auch später kommen. Dann steht oben nur `datentyp name;` und die Zuweisung folgt weiter unten als `name = wert;`. Beides ist richtig.

### Grundlagen: Die wichtigsten Datentypen

- `int` — ganze Zahlen, z. B. Stückzahlen oder ein Alter
- `double` — Zahlen mit Nachkommastellen
- `char` — genau **ein** Zeichen, in einfachen Anführungszeichen
- `boolean` — nur `true` oder `false`
- `String` — Text beliebiger Länge, in doppelten Anführungszeichen

### Merke

In Java heißt die Datei immer wie die Klasse darin: `Steckbrief` wird zu `Steckbrief.java`. Und: **im Code keine Umlaute** — auch nicht in Kommentaren oder in Ausgabetexten. Darauf schaut die Bewertung unter „Clean Code".
'@
    hints          = @(
        'Der Name einer Variablen fängt klein an und beschreibt, was drinsteht — age, nicht a.',
        'Jede Anweisung endet mit einem Semikolon.',
        'Deine IDE unterkringelt Fehler sofort. Lies die Meldung, bevor du etwas änderst — meistens steht die Lösung schon darin.'
    )
}

# ---------------------------------------------------------------- 2
Add-Aufgabe -JUnitDatei 'PersonenprofilTest.java' -Aufgabe @{
    taskCategoryId = $category.id
    title          = 'Der passende Datentyp'
    difficulty     = 0
    order          = 2
    evaluationMode = 1
    expectedTypes  = @(@{ name = 'Personenprofil'; methods = @('public static void main(String[] args)') })
    description    = @'
Jetzt entscheidest du selbst. Die Aufgabe sagt dir, **was** gespeichert werden soll — welcher Datentyp dazu passt, findest du heraus.

**Deine Aufgabe**

Lege die Klasse `Personenprofil` an und speichere zwei Angaben:

- das Körpergewicht in Kilogramm, **mit Nachkommastellen**, in einer Variable namens `weight`
- das Geschlecht als **ein einzelnes Zeichen** (etwa `m`, `w` oder `d`) in einer Variable namens `gender`

Beide bekommen einen sinnvollen Wert. Welchen, ist dir überlassen — geprüft werden Name und Typ.

### Grundlagen: Welcher Typ passt wozu?

- `int` — ganze Zahlen. Nachkommastellen fallen ersatzlos weg.
- `double` — Zahlen mit Nachkommastellen. Der Dezimaltrenner ist der **Punkt**: `1.5`, nicht `1,5`.
- `char` — genau ein Zeichen, in **einfachen** Anführungszeichen: `'a'`
- `String` — Text beliebiger Länge, in **doppelten** Anführungszeichen: `"abc"`
- `boolean` — `true` oder `false`

Die Frage, die du dir bei jeder Variablen stellst: *Welche Werte muss sie aufnehmen können — und welcher Typ kann genau die?*

### Achtung

`'m'` und `"m"` sehen fast gleich aus und sind zwei verschiedene Dinge: das eine ist ein Zeichen, das andere ein Text, der zufällig ein Zeichen lang ist.
'@
    hints          = @(
        'Ein Gewicht wie 72,5 kg passt nicht in eine ganze Zahl — überleg, was dabei verloren ginge.',
        'Zähl die Zeichen: Wie viele muss gender aufnehmen können? Genau eines.',
        'Wenn die IDE „incompatible types" meldet, passt der Wert nicht zum Typ — nicht umgekehrt.'
    )
}

# ---------------------------------------------------------------- 3
Add-Aufgabe -JUnitDatei 'GrundrechenartenTest.java' -Aufgabe @{
    taskCategoryId = $category.id
    title          = 'Addieren und subtrahieren'
    difficulty     = 0
    order          = 3
    evaluationMode = 1
    expectedTypes  = @(@{ name = 'Grundrechenarten'; methods = @('public static void main(String[] args)') })
    description    = @'
Zum ersten Mal rechnet dein Programm — und sagt dir das Ergebnis.

**Deine Aufgabe**

Lege die Klasse `Grundrechenarten` an und rechne mit den beiden Zahlen **17** und **5**:

- Speichere die Summe in einer Variable `sum` vom Typ `int`.
- Speichere die Differenz in einer Variable `difference` vom Typ `int`.
- Gib beide Ergebnisse auf der Konsole aus.

Wie du den Satz drumherum formulierst, ist deine Sache — die beiden Ergebnisse müssen nur in der Ausgabe auftauchen.

### Grundlagen: Rechnen in Java

- `+` addieren, `-` subtrahieren, `*` multiplizieren, `/` dividieren
- Punkt vor Strich gilt wie in der Schule, Klammern setzen sich durch

Ein Ergebnis **ausrechnen** und ein Ergebnis **speichern** sind zwei verschiedene Dinge. `17 + 5` allein verpufft; erst die Zuweisung an eine Variable hält den Wert fest.

### Grundlagen: Etwas ausgeben

`System.out.println(...)` schreibt eine Zeile auf die Konsole. Mit `+` hängst du dabei Text und Zahlen aneinander — steht links ein Text, wird die Zahl automatisch mit angehängt.
'@
    hints          = @(
        'Leg dir zuerst zwei Variablen für 17 und 5 an. Dann liest sich die Rechnung wie der Satz in der Aufgabe.',
        'println schreibt eine Zeile, print bleibt in derselben.',
        'Achte auf die Klammern: "Summe: " + 17 + 5 ergibt nicht das, was du erwartest — probier es ruhig aus.'
    )
}

# ---------------------------------------------------------------- 4
Add-Aufgabe -JUnitDatei 'KommarechnungTest.java' -Aufgabe @{
    taskCategoryId = $category.id
    title          = 'Multiplizieren und dividieren'
    difficulty     = 0
    order          = 4
    evaluationMode = 1
    expectedTypes  = @(@{ name = 'Kommarechnung'; methods = @('public static void main(String[] args)') })
    description    = @'
Dieselbe Übung wie eben, aber mit Kommazahlen — und einer Falle, die in Java jeden einmal erwischt.

**Deine Aufgabe**

Lege die Klasse `Kommarechnung` an und rechne mit den beiden Werten **9.0** und **4.0**:

- Speichere das Produkt in einer Variable `product` vom Typ `double`.
- Speichere den Quotienten in einer Variable `quotient` vom Typ `double`.
- Gib beide Ergebnisse aus.

Erwartet werden `36.0` und `2.25`.

### Grundlagen: Warum der Typ hier über das Ergebnis entscheidet

Java rechnet so, wie die **Werte** es vorgeben, nicht wie die Variable heißt, in der das Ergebnis landet:

- `9 / 4` sind zwei ganze Zahlen. Das Ergebnis ist wieder eine ganze Zahl: **2**. Der Rest wird abgeschnitten, nicht gerundet.
- `9.0 / 4.0` sind zwei Kommazahlen. Das Ergebnis ist **2.25**.

Ein `double` links vom `=` rettet nichts mehr — da ist der Rest längst weg. Entscheidend ist, womit du rechnest.

### Merke

`9.0` und `9` sind für Java nicht dasselbe. Das `.0` ist kein Schmuck, es ist die Typangabe.
'@
    hints          = @(
        'Schreib die Ausgangswerte mit .0 — daran erkennt Java, dass du mit Kommazahlen rechnen willst.',
        'Wenn bei dir 2 statt 2.25 herauskommt, hast du irgendwo zwei ganze Zahlen geteilt.',
        'Der Dezimaltrenner ist der Punkt. Ein Komma trennt in Java Argumente, keine Nachkommastellen.'
    )
}

# ---------------------------------------------------------------- 5
Add-Aufgabe -JUnitDatei 'NamensschildTest.java' -Aufgabe @{
    taskCategoryId = $category.id
    title          = 'Dein Namensschild'
    difficulty     = 0
    order          = 5
    evaluationMode = 1
    expectedTypes  = @(@{ name = 'Namensschild'; methods = @('public static void main(String[] args)') })
    description    = @'
Der erste Text im Programm — und der erste, der dir gehört.

**Deine Aufgabe**

Lege die Klasse `Namensschild` an, speichere **deinen eigenen Namen** in einer Variable `name` vom Typ `String` und begrüße dich damit. Die Ausgabe hat genau diese Form:

```
Hallo, Jermayn!
```

Anstelle von `Jermayn` steht natürlich der Name, den du in `name` abgelegt hast. Komma, Leerzeichen und Ausrufezeichen gehören dazu.

### Grundlagen: Strings

Ein `String` ist Text und steht in doppelten Anführungszeichen. Mit `+` setzt du Texte und Variablen zu einem längeren Text zusammen — das nennt sich **Verkettung**.

Achte auf die Leerzeichen: Java fügt keine von sich aus hinzu. Was zwischen zwei Wörtern stehen soll, muss im Text mit drinstehen.

### Merke

Nimm einen Namen **ohne Umlaute**. Der Zeichensatz-Check schaut auch in Ausgabetexte hinein — das ist keine Schikane, sondern erspart dir die Kodierungsprobleme der Java-Konsole.
'@
    hints          = @(
        'Zwei Bausteine reichen: eine Variable mit deinem Namen und eine Zeile, die sie ausgibt.',
        'Das Ausrufezeichen steht hinter dem Namen — also hinter der Variablen, nicht im selben Text davor.',
        'Wenn zwischen „Hallo," und deinem Namen kein Leerzeichen erscheint: es fehlt im Text, nicht im Code drumherum.'
    )
}

# ---------------------------------------------------------------- 6
Add-Aufgabe -JUnitDatei 'VisitenkarteTest.java' -Aufgabe @{
    taskCategoryId = $category.id
    title          = 'Die Visitenkarte'
    difficulty     = 0
    order          = 6
    evaluationMode = 1
    expectedTypes  = @(@{ name = 'Visitenkarte'; methods = @('public static void main(String[] args)') })
    description    = @'
Jetzt kommen mehrere Texte zusammen — und der fertige Satz bekommt eine eigene Variable.

**Deine Aufgabe**

Lege die Klasse `Visitenkarte` an und speichere:

- einen Vornamen in `firstName`
- einen Nachnamen in `lastName`
- die fertige Begrüßung in `greeting`

`greeting` muss aus `firstName` und `lastName` **zusammengesetzt** werden — nicht als fertiger Satz hingeschrieben. Danach gibst du `greeting` aus:

```
Guten Tag, Ada Lovelace!
```

### Grundlagen: Ein Zwischenergebnis speichern

Bisher hast du direkt in `println` zusammengebaut. Hier entsteht der Satz **vorher** und bekommt einen Namen. Das ist derselbe Gedanke wie bei `sum` in der Additionsaufgabe: Erst rechnen (hier: zusammensetzen), dann speichern, dann ausgeben.

Der Vorteil zeigt sich, sobald du denselben Satz zweimal brauchst — oder ihn zwischendurch prüfen willst.

### Denk daran

Zwischen Vor- und Nachname gehört ein Leerzeichen. Es steht in keiner der beiden Variablen, also musst du es beim Zusammensetzen einfügen.
'@
    hints          = @(
        'Drei Variablen, drei Zeilen — die dritte benutzt die beiden ersten.',
        'Ein Text darf aus beliebig vielen Teilen mit + bestehen. Zähl vorher durch, wie viele Teile dein Satz hat.',
        'Wer den Satz einfach abtippt, kommt am Ergebnis vorbei: die Prüfung schaut nach, ob greeting die beiden Variablen wirklich benutzt.'
    )
}

# ---------------------------------------------------------------- 7
# Ab hier Konsolen-Testfaelle statt JUnit: sobald eine Eingabe im Spiel ist,
# gehoert die Pruefung dorthin - nur dort erscheint die Eingabe im Ergebnis.
Add-Aufgabe -Aufgabe @{
    taskCategoryId = $category.id
    title          = 'Frag nach dem Namen'
    difficulty     = 0
    order          = 7
    evaluationMode = 0
    expectedTypes  = @(@{ name = 'Namensabfrage'; methods = @('public static void main(String[] args)') })
    description    = @'
Bisher standen alle Werte fest im Code. Ab jetzt fragt dein Programm nach.

**Deine Aufgabe**

Lege die Klasse `Namensabfrage` an. Das Programm fragt nach dem Namen, liest ihn ein und begrüßt die Person. Die Ausgabe hat **genau** diese Form:

```
Wie heisst du?
Hallo, Anna!
```

Die erste Zeile ist die Frage, die zweite die Antwort mit dem eingegebenen Namen.

### Grundlagen: Eingaben mit Scanner

Zum Einlesen brauchst du drei Dinge:

1. den Import `java.util.Scanner` ganz oben in der Datei
2. einen `Scanner`, der an `System.in` hängt — den erzeugst du **einmal** am Anfang
3. eine Lesemethode: `nextLine()` für eine ganze Textzeile, `nextInt()` für eine ganze Zahl

Der Rückgabewert der Lesemethode ist der eingegebene Wert. Er verschwindet, wenn du ihn nicht in einer Variablen auffängst.

### Wichtig: Die Ausgabe wird Zeichen für Zeichen verglichen

Fragetext und Begrüßung müssen exakt so lauten wie oben — auch das Fragezeichen, das Komma und das Ausrufezeichen. Nur führende und abschließende Leerzeilen werden ignoriert.
'@
    hints          = @(
        'Erst fragen, dann lesen: sonst sitzt der Benutzer vor einem stummen Programm.',
        'nextLine() liefert die ganze Zeile — auch dann, wenn ein Leerzeichen darin vorkommt.',
        'Der Scanner wird einmal angelegt und danach immer wieder benutzt, nicht vor jeder Frage neu.'
    )
} -Testfaelle @(
    @{
        input          = 'Anna'
        expectedOutput = @'
Wie heisst du?
Hallo, Anna!
'@
        description    = 'Das Programm fragt nach dem Namen und begrüßt „Anna"'
        order          = 1
    },
    @{
        input          = 'Bjarne'
        expectedOutput = @'
Wie heisst du?
Hallo, Bjarne!
'@
        description    = 'Das Programm begrüßt auch einen anderen Namen richtig'
        order          = 2
    }
)

# ---------------------------------------------------------------- 8
Add-Aufgabe -Aufgabe @{
    taskCategoryId = $category.id
    title          = 'Rechnen mit Eingabe'
    difficulty     = 1
    order          = 8
    evaluationMode = 0
    expectedTypes  = @(@{ name = 'Taschenrechner'; methods = @('public static void main(String[] args)') })
    description    = @'
Zwei Zahlen von der Tastatur, vier Ergebnisse auf die Konsole.

**Deine Aufgabe**

Lege die Klasse `Taschenrechner` an. Das Programm fragt nacheinander zwei ganze Zahlen ab und gibt Summe, Differenz, Produkt und Quotienten aus — genau in dieser Form und Reihenfolge:

```
Erste Zahl?
Zweite Zahl?
Summe: 22
Differenz: 12
Produkt: 85
Quotient: 3
```

Das Beispiel zeigt den Durchlauf mit **17** und **5**.

### Denk kurz nach: Warum steht da 3?

17 geteilt durch 5 sind 3,4 — ausgegeben wird trotzdem `3`. Das ist kein Fehler, sondern die Ganzzahl-Division aus der letzten Aufgabe: zwei `int`-Werte ergeben wieder einen `int`, die Nachkommastellen fallen weg. Genau so ist es hier gewollt.

Überleg dir trotzdem: Was müsstest du ändern, damit `3.4` herauskommt?

### Grundlagen: Zahlen einlesen

`nextInt()` liest die nächste ganze Zahl. Jede der beiden Fragen steht in einer eigenen Zeile und kommt **vor** der zugehörigen Eingabe.
'@
    hints          = @(
        'Vier Ausgabezeilen, vier Rechnungen — du brauchst dafür keine vier zusätzlichen Variablen, aber du darfst.',
        'Die Beschriftungen lauten exakt „Summe: ", „Differenz: ", „Produkt: " und „Quotient: ".',
        'Rechnungen in einer Ausgabe gehören in Klammern, sonst hängt Java die Zahlen als Text aneinander.'
    )
} -Testfaelle @(
    @{
        input          = @'
17
5
'@
        expectedOutput = @'
Erste Zahl?
Zweite Zahl?
Summe: 22
Differenz: 12
Produkt: 85
Quotient: 3
'@
        description    = 'Das Programm rechnet mit 17 und 5 alle vier Grundrechenarten'
        order          = 1
    },
    @{
        input          = @'
20
4
'@
        expectedOutput = @'
Erste Zahl?
Zweite Zahl?
Summe: 24
Differenz: 16
Produkt: 80
Quotient: 5
'@
        description    = 'Das Programm rechnet auch mit einer Division ohne Rest richtig'
        order          = 2
    }
)

# ---------------------------------------------------------------- 9
Add-Aufgabe -Aufgabe @{
    taskCategoryId = $category.id
    title          = 'Was übrig bleibt'
    difficulty     = 1
    order          = 9
    evaluationMode = 0
    expectedTypes  = @(@{ name = 'Restrechner'; methods = @('public static void main(String[] args)') })
    description    = @'
Die Ganzzahl-Division schneidet den Rest ab. Diese Aufgabe holt ihn zurück.

**Deine Aufgabe**

Lege die Klasse `Restrechner` an. Das Programm fragt zwei ganze Zahlen ab und erklärt das Ergebnis in einem vollständigen Satz:

```
Erste Zahl?
Zweite Zahl?
17 geteilt durch 5 ergibt Rest 2.
```

Beide eingegebenen Zahlen und der Rest stehen im Satz — der Punkt am Ende gehört dazu.

### Grundlagen: Der Modulo-Operator

`%` liefert den **Rest** einer Division:

- `17 % 5` ist `2`, denn 5 passt dreimal in 17 und 2 bleiben übrig.
- `20 % 4` ist `0` — die Division geht auf.

Genau daran erkennt man später, ob eine Zahl gerade ist oder ob sie durch etwas teilbar ist. Der Operator wird dich noch oft begleiten.
'@
    hints          = @(
        'Der Satz besteht aus fünf Teilen: Zahl, Text, Zahl, Text, Rest — plus dem Punkt.',
        'Du kannst den Rest vorher in einer Variablen speichern oder direkt in der Ausgabe berechnen. Beides ist richtig.',
        'Wenn der Rest 0 ist, geht die Division auf. Der Satz bleibt trotzdem derselbe.'
    )
} -Testfaelle @(
    @{
        input          = @'
17
5
'@
        expectedOutput = @'
Erste Zahl?
Zweite Zahl?
17 geteilt durch 5 ergibt Rest 2.
'@
        description    = 'Das Programm nennt den Rest von 17 geteilt durch 5'
        order          = 1
    },
    @{
        input          = @'
20
4
'@
        expectedOutput = @'
Erste Zahl?
Zweite Zahl?
20 geteilt durch 4 ergibt Rest 0.
'@
        description    = 'Das Programm meldet auch einen Rest von 0 richtig'
        order          = 2
    }
)

# ---------------------------------------------------------------- 10
Add-Aufgabe -Aufgabe @{
    taskCategoryId = $category.id
    title          = 'Hoch und runter'
    difficulty     = 1
    order          = 10
    evaluationMode = 0
    expectedTypes  = @(@{ name = 'Zaehlwerk'; methods = @('public static void main(String[] args)') })
    description    = @'
Eine Variable behält ihren Wert nicht für immer — sie darf sich ändern. Genau davon hat sie ihren Namen.

**Deine Aufgabe**

Lege die Klasse `Zaehlwerk` an. Das Programm fragt einen Startwert ab, zeigt ihn, erhöht ihn um eins, zeigt ihn wieder, verringert ihn um eins und zeigt ihn ein drittes Mal:

```
Startwert?
Vorher: 7
Nach ++: 8
Nach --: 7
```

Wichtig: Es wird **dieselbe** Variable verändert, nicht jedes Mal eine neue angelegt.

### Grundlagen: Inkrement und Dekrement

- `zahl++` erhöht `zahl` um 1
- `zahl--` verringert `zahl` um 1

Beides sind Kurzschreibweisen für `zahl = zahl + 1` beziehungsweise `zahl = zahl - 1`. Solche Zähler sind der Motor jeder Schleife — dort begegnen sie dir wieder.

### Beobachte

Nach `++` und anschließendem `--` steht wieder der Startwert da. Das ist kein Zufall, sondern der Beleg dafür, dass beide Operatoren dieselbe Variable anfassen.
'@
    hints          = @(
        'Eine Variable, drei Ausgaben, dazwischen jeweils eine Änderung.',
        'Die Beschriftungen lauten genau „Vorher: ", „Nach ++: " und „Nach --: ".',
        'Wenn sich der Wert nicht ändert, hast du das Ergebnis vermutlich nirgends gespeichert.'
    )
} -Testfaelle @(
    @{
        input          = '7'
        expectedOutput = @'
Startwert?
Vorher: 7
Nach ++: 8
Nach --: 7
'@
        description    = 'Das Programm zählt von 7 aus hoch und wieder herunter'
        order          = 1
    },
    @{
        input          = '0'
        expectedOutput = @'
Startwert?
Vorher: 0
Nach ++: 1
Nach --: 0
'@
        description    = 'Das Programm rechnet auch ab 0 richtig'
        order          = 2
    }
)

# ---------------------------------------------------------------- 11
Add-Aufgabe -Aufgabe @{
    taskCategoryId = $category.id
    title          = 'GROSS und klein'
    difficulty     = 1
    order          = 11
    evaluationMode = 0
    expectedTypes  = @(@{ name = 'Schreibweise'; methods = @('public static void main(String[] args)') })
    description    = @'
Ein `String` kann mehr, als nur dazustehen: er bringt eigene Fähigkeiten mit.

**Deine Aufgabe**

Lege die Klasse `Schreibweise` an. Das Programm liest eine Textzeile ein und gibt sie zweimal aus — einmal komplett groß, einmal komplett klein:

```
Gib einen Text ein:
GROSS: HALLO WELT
klein: hallo welt
```

### Grundlagen: Methoden auf einem String

An einen `String` hängst du mit einem Punkt eine Fähigkeit an:

- `toUpperCase()` liefert den Text in Großbuchstaben
- `toLowerCase()` liefert ihn in Kleinbuchstaben

Achtung, ein Denkfehler, der oft passiert: Diese Methoden **verändern die Variable nicht**. Sie liefern einen neuen Text zurück. Wer ihn nicht auffängt oder direkt ausgibt, hat nichts davon.

### Denk daran

Der eingegebene Text kann Leerzeichen enthalten. Überleg, welche der beiden Lesemethoden aus Aufgabe 7 damit umgehen kann — die andere hört beim ersten Leerzeichen auf.
'@
    hints          = @(
        'Zwei Ausgabezeilen, dieselbe Variable, zwei verschiedene Methoden darauf.',
        'Die Beschriftungen lauten genau „GROSS: " und „klein: ".',
        'Wenn nur das erste Wort ankommt, liest du mit der falschen Methode ein.'
    )
} -Testfaelle @(
    @{
        input          = 'Hallo Welt'
        expectedOutput = @'
Gib einen Text ein:
GROSS: HALLO WELT
klein: hallo welt
'@
        description    = 'Das Programm gibt „Hallo Welt" in beiden Schreibweisen aus'
        order          = 1
    },
    @{
        input          = 'SoopWorkshop 2026'
        expectedOutput = @'
Gib einen Text ein:
GROSS: SOOPWORKSHOP 2026
klein: soopworkshop 2026
'@
        description    = 'Das Programm lässt Ziffern unverändert und erfasst die ganze Zeile'
        order          = 2
    }
)

Write-Host ''
Write-Host 'Fertig. Musterloesungen liegen unter tests/manual/junit/loesungen/:' -ForegroundColor Green
Write-Host '  tb1-01-steckbrief .. tb1-11-schreibweise   je eine bestandene Abgabe'
Write-Host ''
Write-Host 'Zwei Gegenproben zum Vorfuehren:' -ForegroundColor Green
Write-Host '  tb1-02-personenprofil-falscher-typ   int statt double, String statt char'
Write-Host '  tb1-06-visitenkarte-abgetippt        greeting abgetippt statt zusammengesetzt'
Write-Host ''
