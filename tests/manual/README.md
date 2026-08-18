# Manuelle Tests

Hilfsmittel für den Durchlauf vor einem Merge (Ablauf in `CLAUDE.md` §7).
Kein Teil von `dotnet test` — hier liegt nur, was menschliche Augen oder ein
laufendes System brauchen.

## `abnahme-phase6.md`

Die Abnahme von Phase 6 (Testabdeckung). Sie ist kurz, weil diese Phase keine
Funktionalität geändert hat — der Großteil dessen, was frühere Phasen von Hand
nachklicken ließen, läuft jetzt automatisiert. Enthält die Zahlen, die
gefahrene Gegenprobe und die vier Befunde.

## `abnahme-phase5.md` und `abnahme-phase4.md`

Die Klickanleitungen für die Abnahmen von Phase 5 und 4. Beide zweigeteilt: was
bereits automatisiert geprüft ist (und deshalb nicht nachgeklickt werden muss),
und was menschliche Augen brauchen. Letzteres ist vor allem **Bewegung** —
Animationen lassen sich aus einer nicht gezeichneten Browser-Ansicht heraus
grundsätzlich nicht prüfen, siehe `CLAUDE.md` §6.1.

## `pruefe-uploads.ps1`

Prüft die serverseitige Upload-Validierung und den Status-Endpunkt gegen die
laufende API. Deckt die Fälle ab, die sich im Browser **nicht** auslösen lassen,
weil das Frontend vorher blockt: Dateinamen mit Pfadanteilen, doppelte Namen,
unbekannte Aufgaben-ID.

```powershell
.\tests\manual\pruefe-uploads.ps1
```

Ohne Argumente nimmt es die erste sichtbare Aufgabe aus der API. Am Ende reicht
es eine gültige Datei ein und nennt die URLs zum Verfolgen des Ergebnisses.

Die ungültigen Dateien (zu groß, leer, falsche Endung) erzeugt das Skript im
Speicher — im Repository liegen deshalb keine kaputten Beispieldateien.

## `java/`

Beispielabgaben für den Durchlauf im Browser. Sie sind auf die Aufgabe „Hallo
Welt" mit der erwarteten Ausgabe `Hallo SOOP Workshop!` zugeschnitten.

| Datei | Wofür |
|---|---|
| `Main.java` | korrekte Lösung — muss 100 / 100 ergeben |
| `Endlos.java` | Endlosschleife — muss nach `RunTimeoutSeconds` mit einer verständlichen Meldung abbrechen, ohne verwaisten `java`-Prozess |
| `VielAusgabe.java` | 50 000 Zeilen auf stdout **und** stderr — Gegenprobe zum behobenen Deadlock, muss ohne Hänger durchlaufen |
| `Absturz.java` | Laufzeitfehler ohne vorherige Ausgabe — der Stacktrace muss unter „Erhalten" erscheinen |
| `Umlaute.java` | prüft die Zeichensatzkette Upload → `javac` → `java` → Anzeige; unter „Erhalten" müssen `ä ö ü ß` lesbar stehen |
| `Kaputt.java` | Compilerfehler — die Meldung muss `Kaputt.java:3: error` lauten, ohne das Temp-Verzeichnis des Servers |

Passt eine Aufgabe nicht mehr zu diesen Dateien, gehören sie angepasst — sie
sind Teil der Testanleitung, nicht Beiwerk.

## `seed-phase3.ps1`

Legt gegen die laufende API drei Beispielaufgaben an, die zusammen alle drei
Auswertungsmodi abdecken, und schaltet sie sichtbar:

```powershell
.\tests\manual\seed-phase3.ps1
```

| Aufgabe | Modus | Wofür |
|---|---|---|
| Hallo Soop (Konsole) | `ConsoleOnly` | klassische Konsolen-Testfälle |
| Hallo Soop (Unit-Test) | `UnitTestOnly` | dieselbe Aufgabe über JUnit geprüft — der Teilnehmer schreibt nur eine `main` |
| Rechner | `Both` | eigene Methode plus Ausgabe, beide Prüfarten zusammen |

Mehrfach ausführbar: eine vorhandene Kategorie gleichen Namens wird vorher
gelöscht. Löschen aufräumen geht per `DELETE /api/admin/categories/{id}`.

## `junit/`

### `junit/tests/`

Die beiden JUnit-Vorlagen, die das Seed-Skript hinterlegt. Sie sind zugleich die
Belege dafür, dass sich `main` und Konsolenausgabe aus JUnit heraus prüfen lassen
— wichtig für die frühen Aufgaben ohne eigene Methoden.

| Datei | Zeigt |
|---|---|
| `HalloSoopTest.java` | Ausgabe über `System.setOut` abfangen und `Main.main(...)` aufrufen |
| `RechnerTest.java` | eine eigene Methode prüfen — bewusst **ohne** Eingabesimulation, siehe CLAUDE.md §5.7 |

Die Kommentare darin sind Teil der Vorlage: UTF-8-`PrintStream`, Zurücksetzen in
`@AfterEach` und der statische Zustand zwischen Testmethoden sind genau die
Stolpersteine, über die man sonst zweimal fällt.

### `junit/loesungen/`

Beispielabgaben zu den Aufgaben aus dem Seed-Skript. Jede liegt in einem eigenen
Ordner als `Main.java`, weil Java den Dateinamen an den Klassennamen bindet — die
Ausnahme ist `rechner-falscher-klassenname`, genau darum geht es dort.

| Ordner | Erwartetes Ergebnis |
|---|---|
| `hallo-soop` | Musterlösung für Aufgabe 1 und 2 — 100 / 100 |
| `hallo-soop-tippfehler` | Unit-Test fällt durch, Meldung nennt erwartet gegen erhalten |
| `rechner` | Musterlösung für Aufgabe 3 — 100 / 100 über alle drei Kategorien |
| `rechner-falscher-klassenname` | heißt `Rechner` statt `Main` und liegt deshalb als `Rechner.java` vor — kompiliert und rechnet richtig, muss aber unter Kompilierbarkeit an „Klasse `Main` vorhanden" scheitern |
| `rechner-falscher-methodenname` | Testdatei kompiliert nicht — die Meldung muss die **erwartete Signatur** nennen, nicht nur „cannot find symbol" |
| `rechner-falscher-rueckgabewert` | kompiliert, fällt inhaltlich durch — Konsolen- **und** Unit-Tests rot |
| `rechner-umlaute` | prüft die UTF-8-Kette bis in die Anzeige; Clean Code beanstandet die Umlaute, die Meldung selbst muss lesbar sein |
| `rechner-system-exit` | beendet die JVM des Testlaufs — muss als verständliche Meldung ankommen, nicht als leeres Ergebnis |
