# CLAUDE.md — Arbeits- und Fortschrittsdatei für SoopWorkshop

> Diese Datei ist die **gemeinsame Wahrheit** für die Zusammenarbeit an diesem Projekt.
> Claude liest sie zu Beginn jeder Sitzung und hält die Fortschrittsliste aktuell.
> Stand: 2026-08-15

---

## 1. Was ist SoopWorkshop?

Automatisches Auswertungstool für Java-Aufgaben im SOOP-Workshop (Strukturierte
Objektorientierte Programmierung). Teilnehmer laden `.java`-Dateien hoch, das Tool
kompiliert sie, prüft sie gegen hinterlegte Tests und gibt kategorisiertes Feedback.

Lernprojekt neben der Ausbildung (Fachinformatiker Anwendungsentwicklung).
Betrieb ausschließlich **workshop-intern**, nicht öffentlich erreichbar.

### Zwei Bedeutungen von „Test" — bitte nicht verwechseln

| Begriff in dieser Datei | Was gemeint ist |
|---|---|
| **Aufgaben-Unittests** / JUnit | JUnit-Testdateien, die der Admin pro Aufgabe hinterlegt und die gegen die **abgegebene Java-Lösung** laufen |
| **Konsolen-Testfälle** | stdin/stdout-Vergleich (bestehendes `TaskTest`), z. B. für „HelloWorld" |
| **Projekt-Tests** / xUnit | unsere eigenen C#-Tests in `tests/SoopWorkshop.Tests` |

---

## 2. Arbeitsweise (verbindlich)

Für **jeden** Arbeitsschritt gilt diese Reihenfolge:

1. **Planen** — Vorgehen kurz beschreiben, betroffene Dateien nennen, Unklarheiten fragen.
2. **Umsetzen** — sauber, minimal, im Stil des umgebenden Codes.
3. **Überprüfen** — `dotnet build` + `dotnet test`, bei UI zusätzlich visuell prüfen.
4. **Korrigieren** — Schleife 2–4 wiederholen, bis es funktioniert **und** logisch Sinn ergibt.

Zusätzlich:

- **Struktur vor Feature.** Passt etwas nicht in die Struktur, wird die Struktur
  korrigiert — nicht das Feature reingequetscht.
- **Keine stillen Fehler.** Kein `catch { }`, kein `return null` ohne Aussage.
- **Fortschrittsliste pflegen.** Nach jedem abgeschlossenen Punkt Häkchen setzen und
  neue Findings unter §8 eintragen.
- **Kommentare auf Deutsch**, Code-Bezeichner auf Englisch — wie im bestehenden Code.
- **Keine ungefragten Zusatzfeatures.** Was nicht im Plan steht, wird vorher besprochen.

---

## 3. Befehle

```bash
dotnet build SoopWorkshop.slnx
```

```bash
dotnet test SoopWorkshop.slnx
```

```bash
dotnet run --project src/SoopWorkshop.Backend.API
```

```bash
dotnet run --project src/SoopWorkshop.Frontend.Web
```

| Dienst | HTTP | HTTPS |
|---|---|---|
| Backend API | `http://localhost:5120` | `https://localhost:7212` |
| Frontend Web | `http://localhost:5072` | `https://localhost:7281` |
| Scalar (API-Doku, nur Development) | `http://localhost:5120/scalar` | — |

**Voraussetzungen (lokal):** .NET 10 SDK, PostgreSQL, JDK im `PATH`
(`javac`/`java` werden als Prozess aufgerufen). Ab Phase 7 läuft alles in Docker Compose.

Migrationen:

```bash
dotnet ef database update --project src/SoopWorkshop.Backend.Infrastructure --startup-project src/SoopWorkshop.Backend.API
```

---

## 4. Architektur

Clean Architecture, 7 Projekte + 1 Testprojekt:

```
SoopWorkshop.Shared                  DTOs, Enums, Constants — von allen referenzierbar
SoopWorkshop.Backend.Domain          Entities, ValueObjects — kennt nur Shared
SoopWorkshop.Backend.Application     Services, Interfaces, Result<T> — kennt Domain + Shared
SoopWorkshop.Backend.Infrastructure  EF Core, Repositories, Java-Checker — kennt Application
SoopWorkshop.Backend.API             Controller, Middleware — kennt Application + Infrastructure
SoopWorkshop.Frontend.Services       HttpClients, State — kennt Shared
SoopWorkshop.Frontend.Web            Blazor Server + MudBlazor — kennt Frontend.Services
tests/SoopWorkshop.Tests             xUnit (Projekt-Tests)
```

**Abhängigkeitsregeln (nicht verletzen):**

- Domain kennt **kein** EF Core, **keine** Infrastruktur.
- Application definiert Interfaces (`IJavaAnalyzer`, `I*Repository`), Infrastructure implementiert sie.
- Frontend kennt **nur** `Shared` — niemals Domain oder Application.
- Kommunikation Frontend ↔ Backend ausschließlich über HTTP + DTOs aus `Shared`.

**Kernablauf Auswertung:**

`SubmissionService.CreateAsync` → Warteschlange → `EvaluationService.EvaluateAsync`
→ `JavaAnalyzer` → Checker-Pipeline → `EvaluationResult` persistiert → Frontend pollt.

---

## 5. Zielbild Bewertungs-Engine (Phase 3)

Das ist der fachliche Kern des Projekts und aktuell der größte Umbau.

### 5.1 Zwei Prüfarten pro Aufgabe

Eine Aufgabe kann Konsolen-Testfälle, Aufgaben-Unittests oder beides nutzen:

- **Konsolen-Testfälle** — bestehendes `TaskTest` (Input → erwartete Ausgabe).
  Für frühe Aufgaben, bei denen nur `main` existiert (z. B. „Hallo Soop" ausgeben).
- **Aufgaben-Unittests (JUnit)** — der Admin hinterlegt eine oder mehrere
  JUnit-Testdateien pro Aufgabe. Diese werden zusammen mit der Abgabe kompiliert
  und ausgeführt. Für spätere Aufgaben, bei denen konkrete Methoden und bei
  OOP-Aufgaben mehrere Klassen über mehrere Dateien geprüft werden.

Konsolenausgabe lässt sich auch **innerhalb** von JUnit prüfen (`System.setOut`
umleiten, `Main.main(...)` aufrufen). Dafür soll das Admin-Panel Vorlagen anbieten,
damit man das nicht jedes Mal neu schreibt.

### 5.2 Neue Domänen-Objekte

```
TaskItem
  ├─ EvaluationMode          ConsoleOnly | UnitTestOnly | Both
  ├─ Hints                   (bestehend)
  ├─ Tests                   (bestehend) → Konsolen-Testfälle
  └─ UnitTestFiles           (neu) → JUnit-Quelldateien: FileName, Content, Order
```

### 5.3 Ausführung der JUnit-Tests

- JUnit 5 „Platform Console Standalone"-JAR wird mitgeliefert und im Backend-Image
  abgelegt; Pfad über Konfiguration, keine feste Verdrahtung.
- Ablauf: Abgabe + JUnit-Dateien in ein temporäres Verzeichnis schreiben →
  `javac` mit JUnit im Classpath → Console Launcher ausführen → **XML-Report**
  einlesen (nicht stdout parsen, das ist zu brüchig) → pro Testmethode ein
  `TestCaseResult`.
- Exakte CLI-Flags und JUnit-Version werden in Phase 3 verifiziert und hier dokumentiert.
- Kompiliert die JUnit-Datei nicht gegen die Abgabe (z. B. falscher Klassen- oder
  Methodenname), ist das ein legitimes Nichtbestehen — die Fehlermeldung muss
  dem Teilnehmer aber verständlich sagen, **was** erwartet wurde.

### 5.4 Checker-Pipeline statt fester Verdrahtung

`JavaAnalyzer` ruft aktuell vier Checker fest verdrahtet auf. Zielbild:
Interface `IEvaluationChecker` mit `Category`, `Order` und
`CheckAsync(EvaluationContext)`. Der Kontext trägt Abgabe, Aufgabe,
Kompilierergebnis und Arbeitsverzeichnis. Neue Prüfungen werden dann nur noch
registriert — das ist die Voraussetzung dafür, Clean Code sinnvoll auszubauen.

### 5.5 Punktesystem v2

Aktuell feste Konstanten (5/10/20/65 = 100) mit drei Problemen:

- Aufgaben **ohne** Testfälle geben jedem 65 Gratispunkte (`TestCaseChecker`,
  `tests.Count == 0` → volle Punktzahl).
- Ganzzahl-Division verliert Punkte (65 / 3 = 21, 3 × 21 = 63).
- Nicht pro Aufgabe anpassbar, obwohl Aufgaben sehr unterschiedlich sind.

Neue Regeln:

1. Jede Kategorie hat ein **Gewicht**; Standardgewichte global, pro Aufgabe überschreibbar.
2. Kategorie-Ergebnis = Gewicht × (bestandene Teilprüfungen ÷ Teilprüfungen gesamt), als `double`.
3. Kategorien, die für eine Aufgabe **nicht anwendbar** sind (keine Konsolen-Testfälle,
   keine JUnit-Datei), fallen komplett raus; ihr Gewicht wird auf die übrigen verteilt.
   **Keine Gratispunkte.**
4. Endpunkte auf ganze Zahlen mit Restverteilung (größter Rest), Summe exakt 100.
5. `Passed` je Kategorie = alle Teilprüfungen bestanden.

### 5.6 Clean Code

`EvaluationCategory.CleanCode` ist eine **Sammelkategorie**. Darunter fallen
Teilprüfungen wie Namenskonventionen, Zeichensatz und später weitere. Das passt zur
bestehenden Struktur `CategoryResult → viele TestCaseResults`.

Offen: ob `CharacterSet` und `NamingConventions` als eigene Kategorien bleiben oder
als Teilprüfungen unter Clean Code wandern — siehe §9.

### 5.7 Sortierung der Anzeige

Ergebnisse erscheinen im Frontend derzeit in beliebiger Reihenfolge. Zu ergänzen:

- feste Anzeigereihenfolge der Kategorien (`DisplayOrder` in `Shared`)
- `Order` auf `TestCaseResult`, damit Teilprüfungen stabil sortiert sind
- Sortierung im Frontend anwenden, nicht auf DB-Reihenfolge verlassen

---

## 6. Konventionen

- **Ordner = Feature**, nicht Technik: `Tasks/`, `Submissions/`, `Evaluation/` mit je
  `Interfaces/` und `Services/`.
- **Razor-Komponenten** immer mit Code-Behind (`X.razor` + `X.razor.cs`), Styles als
  `X.razor.css` (CSS-Isolation). Keine `@code`-Blöcke in `.razor`.
- **Services geben `Result<T>` zurück**, keine Exceptions für erwartbare Fehlerfälle.
- **DTOs**: `Shared/DTOs/<Bereich>/`. Lese-DTOs unter `Tasks/`, Schreib-DTOs unter `Admin/`.
- **Namensschema Frontend-Seiten**: `Components/Pages/<Bereich>/<Name>.razor`.
- **Nullable + ImplicitUsings** sind überall an — so lassen.

---

## 7. Roadmap

Legende: `[ ]` offen · `[~]` in Arbeit · `[x]` erledigt & geprüft

### Phase 0 — Fundament & Aufräumen
*Sauberer Boden, bevor Features draufgebaut werden. Klein, schnell erledigt.*

- [ ] `Directory.Build.props` (TargetFramework, Nullable, ImplicitUsings zentral)
- [ ] `Directory.Packages.props` (Central Package Management — Versionen stehen 8× dupliziert)
- [ ] **Bug:** CORS-Origins in `Backend.API/Program.cs` haben vertauschte Schemata
      (`https://localhost:5072` / `http://localhost:7281` statt umgekehrt)
- [ ] HttpClient-Registrierung konsolidieren — doppelt in
      `Frontend.Services/DependencyInjection.cs` **und** `Frontend.Web/Program.cs`;
      BaseAddress gehört in `AddFrontendServices(apiBaseUrl)`
- [ ] `UpdateTaskTestDto` wird als Lese-DTO missbraucht → echtes `TaskTestDto` in `DTOs/Tasks/`
- [ ] Toter Code: `Domain/ValueObjects/Score.cs`, `DTOs/Submissions/SubmissionFileDto.cs`,
      `ITaskItemService.GetVisibleByCategoryAsync` — nutzen oder entfernen
- [ ] Ordner `StateManagment` → `StateManagement`
- [ ] Connection-String mit Passwort aus `appsettings.json` → User Secrets
- [ ] MudBlazor-Warnungen in `TaskDetail.razor` beheben (RZ10012 + MUD0002)

### Phase 1 — Projekt-Testfundament (xUnit)
*Nur das Gerüst, keine volle Abdeckung. Absichert die Umbauten in Phase 2–3.*

- [ ] `UnitTest1.cs` entfernen, Ordnerstruktur spiegelt Produktivcode
      (`Unit/Application/`, `Unit/Infrastructure/`, `Integration/`, `Components/`)
- [ ] Pakete: NSubstitute, Shouldly, bUnit, `Microsoft.AspNetCore.Mvc.Testing`
- [ ] Testprojekt referenziert zusätzlich `Shared`, `Backend.API`, `Frontend.Services`
- [ ] Erste echte Tests: `CharacterSetChecker`, `NamingConventionChecker`, `Result<T>`
- [ ] Benennung festlegen: `Methode_Szenario_Erwartung`

### Phase 2 — Backend-Härtung
*Voraussetzung für zuverlässiges Ausführen von JUnit in Phase 3.*

- [ ] **Neuer Endpunkt `GET /api/submissions/{id}/status`** — `/result` liefert derzeit
      bei „läuft noch" und „nicht gefunden" beides 404; das Frontend kann `Failed`
      deshalb nicht erkennen und pollt endlos
- [ ] Auswertung von `_ = Task.Run(...)` auf `BackgroundService` + `Channel`-Queue
      (aktuell gehen laufende Auswertungen bei Neustart verloren, keine Begrenzung
      paralleler JVM-Prozesse)
- [ ] Prozess-Ausführung hinter `IProcessRunner` abstrahieren — testbar **und**
      Voraussetzung dafür, später auf Container-Ausführung umzustellen
- [ ] `WaitForExit(int)` blockiert synchron im async-Pfad → `WaitForExitAsync` + `CancellationToken`
- [ ] `TestCaseChecker` liest `StandardError` nicht → Deadlock-Risiko bei vollem Puffer
- [ ] Upload-Validierung im `SubmissionsController`: Endung `.java`, Max-Größe,
      Max-Anzahl (bisher nur clientseitig auf 1 MB begrenzt)
- [ ] `CleanupWorkingDirectory` fängt nur `IOException` → auch `UnauthorizedAccessException`
- [ ] `CreateTaskItemDto.TaskCategoryId` wird nicht auf Existenz geprüft
- [ ] Strukturiertes Logging (`ILogger`) in Application-Services

### Phase 3 — Bewertungs-Engine v2
*Der fachliche Kern. Details in §5.*

- [ ] `EvaluationMode` auf `TaskItem` (ConsoleOnly | UnitTestOnly | Both)
- [ ] Entität `TaskUnitTestFile` + EF-Konfiguration + Migration
- [ ] JUnit-Standalone-JAR einbinden, Pfad konfigurierbar, Version dokumentieren
- [ ] `JUnitChecker`: kompilieren mit JUnit im Classpath, ausführen, **XML-Report** parsen
- [ ] Kompilierfehler der Testdatei in verständliches Teilnehmer-Feedback übersetzen
- [ ] Checker-Pipeline: `IEvaluationChecker` + `EvaluationContext`, `JavaAnalyzer` iteriert
- [ ] Punktesystem v2: Gewichte, Gewichtsverteilung bei nicht anwendbaren Kategorien,
      Restverteilung — **behebt die 65 Gratispunkte bei Aufgaben ohne Testfälle**
- [ ] Clean Code als Sammelkategorie ausarbeiten (siehe §9, Punkt 1)
- [ ] `DisplayOrder` für Kategorien, `Order` für `TestCaseResult`
- [ ] Admin-Endpunkte für `TaskUnitTestFile` (CRUD)
- [ ] Projekt-Tests für Punkteberechnung und XML-Parsing (reine Funktionen, gut testbar)

### Phase 4 — Frontend Teilnehmer-Sicht

- [ ] `app.css` entrümpeln — enthält noch Bootstrap-Boilerplate aus der Projektvorlage
- [ ] `NotFound.razor` und `Error.razor` auf MudBlazor umstellen (aktuell rohes HTML)
- [ ] `ThemeService`: Auswahl in `localStorage` persistieren; Service startet mit `Light`,
      `MainLayout` initialisiert aber `Dark` → Flackern beim ersten Render
- [ ] Drawer ist fest `Open="true"` → Toggle im AppBar, Verhalten auf Mobil klären
- [ ] Sidebar: Aufgaben innerhalb einer Kategorie nach `Order` sortieren (fehlt)
- [ ] Zentrale Fehlerbehandlung: `GetFromJsonAsync` wirft bei nicht erreichbarer API
      unbehandelt → Snackbar + Retry statt weißer Seite
- [ ] Lade- und Leerzustände vereinheitlichen (Skeletons statt nackter Spinner)
- [ ] `TaskDetail`: Drag & Drop, Dateiliste mit Entfernen-Button, client-seitige Validierung
- [ ] `SubmissionResult`: Status `Pending`/`Running`/`Failed` unterscheiden (setzt Phase 2 voraus),
      „Erneut versuchen", Zurück-Link zur *richtigen* Aufgabe (geht aktuell nach `/`)
- [ ] `SubmissionResult`: JUnit-Ergebnisse darstellen — Testmethode, Erwartung, Fehlermeldung
- [ ] Sortierte Anzeige der Kategorien und Teilprüfungen (setzt Phase 3 voraus)
- [ ] `SubmissionPollingState` sauber verdrahten: als Scoped registriert, aber in
      `SubmissionResult` manuell `new`'d — eine Quelle der Wahrheit; Timeout nach n Versuchen
- [ ] Aufgabenübersicht als eigene Seite (nicht nur Sidebar)
- [ ] Responsive-Durchgang: Mobil, Tablet, Desktop
- [ ] Barrierefreiheit: Fokus-Reihenfolge, Kontraste in allen drei Themes

### Phase 5 — Admin-Panel

- [ ] **Auth:** festes Passwort aus Konfiguration/Env; API prüft statisches Token,
      Frontend hat Login-Seite. Da Blazor Server serverseitig läuft, verlässt das
      Token nie den Browser. `api/admin/*` ist aktuell **komplett offen**.
- [ ] Eigenes `AdminLayout` unter `/admin` mit eigener Navigation
- [ ] Kategorien: Liste, Anlegen, Bearbeiten, Löschen, Sichtbarkeit umschalten
- [ ] Aufgaben: CRUD inkl. Hints, Schwierigkeitsgrad und `EvaluationMode`
- [ ] Konsolen-Testfälle: CRUD pro Aufgabe
- [ ] **JUnit-Dateien: CRUD pro Aufgabe mit Code-Editor** (monospace + Zeilennummern;
      Monaco als spätere Ausbaustufe)
- [ ] **Vorlagen-Bibliothek** für häufige Testmuster: Konsolenausgabe prüfen,
      stdin simulieren, Methoden-Rückgabewert prüfen, mehrere Klassen prüfen
- [ ] Gewichtung der Bewertungskategorien pro Aufgabe einstellbar
- [ ] Reihenfolge (`Order`) bearbeitbar — idealerweise Drag & Drop
- [ ] Vorschau: Aufgabe so anzeigen, wie Teilnehmer sie sehen
- [ ] **Probelauf:** eigene Musterlösung hochladen und Bewertung prüfen, ohne die
      Aufgabe sichtbar zu schalten — sonst merkt man kaputte JUnit-Dateien erst live
- [ ] Bestätigungsdialoge vor dem Löschen
- [ ] Formularvalidierung gegen die DataAnnotations aus `Shared/DTOs/Admin`
- [ ] Optional: Submissions-Übersicht mit Auswertungshistorie

### Phase 6 — Projekt-Testabdeckung ausbauen

- [ ] Unit: alle Checker inkl. `JUnitChecker` (über `IProcessRunner` aus Phase 2)
- [ ] Unit: `TaskCategoryService`, `TaskItemService`, `TaskTestService`,
      `SubmissionService`, `EvaluationService` mit gemockten Repositories
- [ ] Unit: Punkteberechnung mit Randfällen (0 Tests, alle bestanden, Restverteilung)
- [ ] Unit: Mapping-Logik (Entity ↔ DTO)
- [ ] Integration: Controller über `WebApplicationFactory`
- [ ] Integration: Repositories gegen echte PostgreSQL (Testcontainers)
- [ ] Component: bUnit für `TaskSidebarList`, `TaskDetail`, `SubmissionResult`, Admin-Formulare
- [ ] GitHub Actions: Build + Test bei jedem Push
- [ ] Coverage-Report (coverlet ist bereits eingebunden)

### Phase 7 — Docker Compose, README, Abschluss

- [ ] `Dockerfile` Backend — inkl. JDK und JUnit-Standalone-JAR
- [ ] `Dockerfile` Frontend
- [ ] `docker-compose.yml`: PostgreSQL + Backend + Frontend, Healthchecks, `depends_on`,
      benanntes Volume für die DB
- [ ] Konfiguration vollständig über Umgebungsvariablen (ConnectionString, ApiBaseUrl,
      Admin-Passwort) — nichts Fest-Verdrahtetes mehr
- [ ] Migrationen beim Start anwenden oder dokumentierter Einzelschritt
- [ ] **README final**: Was das Tool ist, Voraussetzungen, Setup in einem Befehl,
      DB-Einrichtung, Admin-Login, Aufgaben anlegen, Troubleshooting.
      Die leeren Abschnitte (Architektur, Projektstruktur, Roadmap) füllen.
- [ ] Hinweis in der README: workshop-interner Betrieb, keine Härtung gegen böswillige Abgaben

### Definition of Done — wann ist das Projekt „final"?

- Ein Teilnehmer kann eine Aufgabe öffnen, `.java`-Dateien hochladen und bekommt ein
  sortiertes, verständliches Ergebnis — auch im Fehlerfall.
- Ein Admin kann ohne DB-Zugriff Kategorien, Aufgaben, Hints, Konsolen-Testfälle und
  JUnit-Dateien anlegen, testen und sichtbar schalten.
- Bewertung ist nachvollziehbar, ohne Gratispunkte und ohne Rundungsverluste.
- `dotnet test` läuft grün und deckt Checker, Punkteberechnung und Services ab.
- `docker compose up` startet das komplette System; die README reicht aus, damit
  jemand anderes es aufsetzen kann.

---

## 8. Findings-Log

Format: `Datum — Datei — Beschreibung — geplant für Phase X`

- 2026-08-15 — `Backend.API/Program.cs` — CORS-Schemata vertauscht — Phase 0
- 2026-08-15 — `Frontend.Services/DependencyInjection.cs` — doppelte HttpClient-Registrierung — Phase 0
- 2026-08-15 — `Frontend.Services/StateManagment/` — Ordnername verschrieben — Phase 0
- 2026-08-15 — `Application/Submissions/Services/SubmissionService.cs` — Fire-and-Forget-Auswertung ohne Persistenz-Garantie — Phase 2
- 2026-08-15 — `Frontend.Services/.../SubmissionPollingState.cs` — Endlos-Polling bei `SubmissionStatus.Failed` — Phase 2/4
- 2026-08-15 — `Infrastructure/Evaluation/Checkers/TestCaseChecker.cs` — Aufgaben ohne Testfälle geben 65 Gratispunkte — Phase 3
- 2026-08-15 — `Infrastructure/Evaluation/Checkers/TestCaseChecker.cs` — Ganzzahl-Division verliert Punkte — Phase 3
- 2026-08-15 — `Infrastructure/Evaluation/Checkers/NamingConventionChecker.cs` — Regex prüft auch Strings und Kommentare → False Positives — Phase 3
- 2026-08-15 — `Application/Tasks/Services/TaskCategoryService.cs` — `MapToDto` sortiert Tasks nicht nach `Order` — Phase 4
- 2026-08-15 — `Backend.API/Controllers/Admin/*` — keinerlei Zugriffsschutz — Phase 5

---

## 9. Offene Entscheidungen

1. **Clean-Code-Zuschnitt.** Bleiben `CharacterSet` und `NamingConventions` eigene
   Kategorien, oder wandern sie als Teilprüfungen unter Clean Code? Zweites ergibt
   fachlich mehr Sinn, ändert aber die Anzeige und die Punkteverteilung.
   → Kann in Phase 3 entschieden werden, blockiert vorher nichts.
2. **JUnit-Dateien für Teilnehmer sichtbar?** Wenn ja, sehen sie genau, was geprüft
   wird — lehrreich, aber sie können auf den Test hin schreiben. Pro Aufgabe schaltbar?
3. **Aufgaben-Vertrag.** Woher weiß der Teilnehmer, wie Klasse und Methoden heißen
   müssen, damit die JUnit-Datei kompiliert? Nur in der Beschreibung, oder ein
   strukturiertes Feld („Erwartete Signaturen"), das im Frontend hervorgehoben wird?
4. **Sandbox-Tiefe.** Docker Compose containerisiert die *Anwendung*, isoliert aber
   nicht die einzelne Abgabe — `javac`/`java` laufen im Backend-Container.
   Für einen workshop-internen Betrieb vertretbar. Echte Isolation pro Abgabe
   (eigener Container je Auswertung) wäre eine spätere Ausbaustufe.
   → Vorschlag: v1 mit Timeouts und Prozesslimits, Hinweis in der README.
5. **Datenbank in Tests.** Testcontainers (realistisch, braucht Docker) oder
   EF InMemory (schnell, weicht aber von PostgreSQL ab)?
