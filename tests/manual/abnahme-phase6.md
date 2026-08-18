# Abnahme Phase 6 — Projekt-Testabdeckung

Stand: 2026-08-18, Branch `phase-6-testabdeckung`.

Phase 6 hat **keine Funktionalität geändert**. Weder Teilnehmer noch Betreuer
erleben etwas anders. Entsprechend kurz ist der Teil, den menschliche Augen
brauchen — der Rest steht als automatisierte Prüfung im Repository.

---

## 1. Was jetzt automatisiert geprüft wird

Ein Befehl für alles:

```powershell
.\scripts\pruefe-alles.ps1
```

Er baut, testet, baut das Frontend (darin `tsc -b`), lässt die Frontend-Tests
laufen und prüft den Linter. Am Ende steht eine Zusammenfassung mit Dauer je
Schritt. Er bricht **nicht** beim ersten Fehler ab — wer fünf Prüfungen laufen
lässt, will alle fünf Ergebnisse sehen.

| Schalter | Wirkung |
|---|---|
| `-OhneDocker` | lässt die Integrationstests aus (die brauchen Docker) |
| `-MitCoverage` | erzeugt Coverage-Berichte unter `artifacts/coverage/` |

Läuft das Backend noch, sagt das Skript das vorher und bricht ab, statt in
`CS2012 … used by another process` zu laufen.

### Zahlen

| | Anzahl |
|---|---|
| Projekt-Tests gesamt | **386** (vorher 270) |
| davon Unit (ohne Docker) | 302 |
| davon Integration (Testcontainers) | 84 |
| Frontend-Tests (Vitest) | **145** in 10 Dateien |

Laufzeit: kompletter Lauf rund 20 Sekunden, davon etwa 6 Sekunden für den
PostgreSQL-Container. Ohne Docker rund 8 Sekunden.

### Coverage

Gemessen, **nicht** als Schwelle erzwungen (Entscheidung dieser Phase).

| | Line | Branch |
|---|---|---|
| Backend | 89 % | 59,5 % |
| Frontend | 20 % | 31 % |

Die 20 % im Frontend sind ehrlich und kein Versehen: getestet sind die
Stellen, die in Phase 4 und 5 auffällig waren — Polling-Hook, API-Client,
`checkFiles`, Mapper, Validierung, Gewichte, die §5.7-Darstellung, `ResultView`,
`SubmissionForm`, `ResultPage`. Die Admin-Seiten und die Editoren sind bewusst
nicht abgedeckt; sie werden vom Betreuer bedient und ändern sich noch.
`coverage.include` ist gesetzt, damit ungetestete Dateien mit 0 % in der Zahl
stehen statt aus ihr zu verschwinden.

---

## 2. Was menschliche Augen brauchen

### 2.1 Erster Lauf auf dieser Maschine

Beim allerersten Integrationstest zieht Docker das Image `postgres:17-alpine`
(einmalig, ein bis zwei Minuten). Danach kostet der Container nur noch Sekunden.

- [ ] `.\scripts\pruefe-alles.ps1` läuft durch, Zusammenfassung ist grün
- [ ] `.\scripts\pruefe-alles.ps1 -OhneDocker` läuft ebenfalls durch und ist
      spürbar schneller
- [ ] `.\scripts\pruefe-alles.ps1 -MitCoverage` erzeugt
      `artifacts/coverage/backend/index.html` und
      `artifacts/coverage/frontend/index.html`; beide lassen sich im Browser
      öffnen
- [ ] Die Umlaute und Rahmenzeichen in der Ausgabe des Skripts stehen richtig
      da (`──`, `Prüfungen`) — nicht als `â”€`. Das Skript hat dafür eine BOM,
      und die geht beim Bearbeiten mit manchen Werkzeugen verloren

### 2.2 Backend läuft noch

- [ ] `.\scripts\start-dev.ps1`, dann `.\scripts\pruefe-alles.ps1` — es muss
      **vorher** die Meldung kommen, dass das Backend seine DLLs hält, statt
      dass der Build mitten im Lauf scheitert

### 2.3 Solution in der IDE

Die Kommandozeile deckt die IDE-eigene Auflösung von `Directory.Build.props`
und `Directory.Packages.props` nicht ab. Phase 6 hat dort drei Pakete ergänzt.

- [ ] Solution in Visual Studio bzw. Rider öffnen und bauen — ohne Warnungen
- [ ] Der Test-Explorer zeigt beide Gruppen; ein einzelner Integrationstest
      lässt sich von dort starten

### 2.4 Der Basis-Durchlauf aus CLAUDE.md §7

Unverändert gültig. Er prüft, dass die Anwendung selbst noch tut, was sie soll —
Phase 6 hat daran nichts geändert, aber genau das will belegt sein.

- [ ] Schritte 1 bis 8 aus §7 durchlaufen
- [ ] `git status` sauber; `artifacts/` taucht **nicht** auf (steht in
      `.gitignore`)

---

## 3. Gegenprobe — schon gefahren

Ein Test, der nie rot war, beweist nichts. Für jede der tragenden Zusicherungen
wurde der Produktivcode gezielt gebrochen und geprüft, dass **genau** die
erwarteten Tests fallen; danach zurückgenommen.

| Gebrochen | Erwartet gefallen | Ergebnis |
|---|---|---|
| Sortierung nach `Order` aus `TaskCategoryService.MapToDto` | 1 Test | nur dieser |
| Filter auf `IsVisibleToParticipant` aus `TaskItemService.MapToDto` | 1 Test | nur dieser |
| `Include(CategoryWeights)` aus `TaskItemRepository.GetByIdAsync` | 1 Test | nur dieser |
| Transaktion in `TaskTransferService` (Commit statt Rollback) | Rollback-Test | gefallen |
| `Erhalten`-Beschriftung aus `CategoryCard` | 3 Tests der §5.7-Kopplung | genau diese |

Nicht als Gegenprobe geeignet war der Wächter in `TaskItemRepository.UpdateAsync`
— siehe Punkt 4.

---

## 4. Befunde aus dieser Phase

Alle vier stehen im Findings-Log (CLAUDE.md §9). Keiner davon ist ein Fehler,
den ein Teilnehmer heute merkt.

1. **Der Eintrag zu `TaskItemRepository.UpdateAsync` war falsch.** Er behauptet,
   `Update()` auf einer verfolgten Entität schreibe den ganzen Graphen neu.
   Nachgemessen: EFs Traversierung hält an bereits verfolgten Knoten an, die
   Kinder bleiben `Unchanged`. Der beschriebene Schaden tritt nur bei einer
   **losgelösten** Entität ein — und dort ist der Aufruf nötig. Der Wächter
   bleibt richtig, seine Begründung war es nicht. Kommentar korrigiert, beide
   Fälle als Test festgehalten.

2. **`checkFiles` und der Server sind sich bei der Schreibweise nicht einig.**
   Das Frontend vergleicht Dateinamen bitgenau, `SubmissionUploadValidator` mit
   `OrdinalIgnoreCase`. `A.java` und `a.java` kommen im Browser ohne Warnung
   durch und werden erst vom Server abgelehnt. Beide Seiten sind als
   Ist-Verhalten festgehalten.

3. **`ToggleVisibility` antwortet mit 404, wenn Testdaten fehlen.** Die Aufgabe
   gibt es sehr wohl; der Controller bildet nur jeden Fehlschlag auf `NotFound`
   ab. Sichtbar wird das heute nicht, weil das Panel die Meldung unabhängig vom
   Ausgang anzeigt. Sauber wäre 400 — das ist eine Vertragsänderung und wurde
   deshalb nicht nebenbei erledigt.

4. **`TaskUnitTestFileService.SaveAllAsync` lässt doppelte `Order`-Werte zu**,
   `TaskTestService.SaveAllAsync` nicht. Bei gleichem Wert entscheidet die
   Datenbank über die Anzeigereihenfolge — genau die Begründung, mit der es beim
   Schwesterservice abgelehnt wird.

**Kein Befund war die Restverteilung in `admin/weights.ts`.** Der Verdacht aus
der Planung (`candidates[step % candidates.length]` könnte doppelt vergeben) hat
sich beim Nachrechnen aufgelöst: jeder Eintrag verliert beim Abrunden weniger
als 1, der Rest ist also stets kleiner als die Zahl der Kandidaten. Das Backend
rechnet in `EvaluationScorer.LargestRemainder` identisch. Als Test festgehalten.

---

## 5. Was bewusst offen bleibt

- **GitHub Actions** — auf Phase 7 verschoben, wo es zu den Dockerfiles passt.
  Bei einem Betreuer greift der Hauptnutzen von CI („bricht bei jemand anderem")
  kaum; das Sammelskript deckt den Alltag ab.
- **Keine Coverage-Schwelle** — gemessen wird, blockiert wird nicht.
- **Kein Playwright** — der Durchlauf aus §7 bleibt der Ende-zu-Ende-Nachweis.
- **Admin-Seiten ohne Komponententests** — sie ändern sich noch und werden vom
  Betreuer bedient, nicht von Teilnehmern.
