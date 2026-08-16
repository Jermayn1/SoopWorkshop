# Archiv — stillgelegtes Blazor-Frontend

Hier liegt das erste Frontend (Blazor Server + MudBlazor), **stillgelegt am 2026-08-16**.
Es ist aus der Solution genommen, aber vollständig erhalten.

## Warum stillgelegt

Das Ergebnis hat optisch nicht überzeugt. Rückblickend gab es dafür einen konkreten
Grund: MudBlazor setzt Material Design um — Schatten für Tiefe, großzügige Abstände,
Farbrollen für Zustände. Das angestrebte Erscheinungsbild war in fast jedem Punkt das
Gegenteil (1px-Kanten statt Schatten, kompakte Dichte, ein einzelner Akzent). Etappe 4.1
bestand deshalb zu weiten Teilen darin, die Bibliothek gegen ihre eigenen Annahmen zu
biegen: Elevation überall auf 0, Radien einzeln nachgezogen, Versalien abgeschaltet,
globale Overrides auf MudBlazor-Klassen in `app.css`.

Die Entscheidung, welches Frontend an die Stelle tritt, wird getrennt getroffen.

## Was hier drin ist

```
SoopWorkshop.Frontend.Web         Blazor Server + MudBlazor, Teilnehmer-Sicht
SoopWorkshop.Frontend.Services    HttpClients, ApiResult<T>, ThemeService, Polling
```

## Was davon weiterlebt

Das Backend ist unberührt. Fachlich wiederverwendbar sind vor allem die Erkenntnisse,
nicht der Code:

- **`SubmissionPollingState`** — 2 s Intervall, Abbruch nach 150 Versuchen, sauberes
  Verhalten bei `Pending` / `Running` / `Failed` / `Done`. Diese Zustandsmaschine muss
  jedes neue Frontend genauso nachbauen.
- **`ApiResult<T>`** — die Erkenntnis, dass die Oberfläche drei Ausgänge unterscheiden
  muss: Erfolg, *gibt es nicht*, *nicht erreichbar*. Ein blosses `null` warf die letzten
  beiden zusammen und behauptete bei gestopptem Backend, die Aufgabe sei gelöscht.
- **`SubmissionResult.razor` + `.razor.css`** — die Darstellung einer Teilprüfung
  (Eingabe / Erwartet / Erhalten, rote Kante statt roter Schrift wegen Kontrast). Die
  Regeln dahinter stehen in CLAUDE.md §5.7 und gelten unabhängig vom Framework.
- **Die Upload-Grenzen** kommen aus `Shared/Constants/SubmissionUploadLimits.cs` und
  bleiben dort — ein neues Frontend muss sie über die API oder eine eigene Konstante
  spiegeln.

## Wieder aktivieren

1. Beide Projekte zurück nach `src/` schieben.
2. In `SoopWorkshop.Frontend.Services.csproj` den Pfad zu `Shared` von
   `..\..\src\` auf `..\` zurücksetzen.
3. Beide Projekte in `SoopWorkshop.slnx` eintragen.
4. In `tests/SoopWorkshop.Tests.csproj` die `ProjectReference` auf `Frontend.Services`
   wieder ergänzen (aktuell nutzt kein Test sie — sie stand nur da).
5. In `scripts/start-dev.ps1` den Frontend-Block wieder aktivieren.

Der Stand entspricht dem Ende von Etappe 4.1 (Branch `phase-4-1-fundament`,
Commits `66acfe8`, `465f054`, `63c9426`, `1e6139f`).
