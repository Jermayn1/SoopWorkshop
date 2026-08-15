# Manuelle Tests

Hilfsmittel für den Durchlauf vor einem Merge (Ablauf in `CLAUDE.md` §7).
Kein Teil von `dotnet test` — hier liegt nur, was menschliche Augen oder ein
laufendes System brauchen.

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
