# CLAUDE.md — Arbeits- und Fortschrittsdatei für SoopWorkshop

> Diese Datei ist die **gemeinsame Wahrheit** für die Zusammenarbeit an diesem Projekt.
> Claude liest sie zu Beginn jeder Sitzung und hält die Fortschrittsliste aktuell.
> Stand: 2026-08-15 — Phase 0, 1 und 2 abgeschlossen, als Nächstes Phase 3.

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

- **Vor jedem Merge eine Testanleitung.** Am Ende einer Phase liefert Claude eine
  konkrete Klickanleitung mit erwartetem Ergebnis pro Schritt — inklusive der
  nötigen Testdaten und Beispieldateien. Was Claude bereits automatisiert geprüft
  hat, wird getrennt ausgewiesen; die Anleitung enthält nur, was menschliche Augen
  brauchen. Der Basis-Durchlauf steht in §7, phasenspezifische Schritte kommen dazu.
- **Struktur vor Feature.** Passt etwas nicht in die Struktur, wird die Struktur
  korrigiert — nicht das Feature reingequetscht.
- **Keine stillen Fehler.** Kein `catch { }`, kein `return null` ohne Aussage.
- **Fortschrittsliste pflegen.** Nach jedem abgeschlossenen Punkt Häkchen setzen und
  neue Findings unter §9 eintragen.
- **Kommentare auf Deutsch**, Code-Bezeichner auf Englisch — wie im bestehenden Code.
- **Keine ungefragten Zusatzfeatures.** Was nicht im Plan steht, wird vorher besprochen.

---

## 3. Befehle

Alles starten (Datenbank, Build, Backend, Frontend):

```bash
.\scripts\start-dev.ps1
```

```bash
.\scripts\stop-dev.ps1
```

Datenbank-Passwort an die `.env` angleichen (siehe unten):

```bash
.\scripts\sync-db-password.ps1
```

`start-dev.ps1 -SkipBuild` überspringt den Build, `-NoDatabase` lässt den Container in Ruhe.
Backend und Frontend laufen in je einem eigenen Fenster, damit die Logs getrennt lesbar sind.

Einzeln:

```bash
dotnet build SoopWorkshop.slnx
```

```bash
dotnet test SoopWorkshop.slnx
```

```bash
docker compose up -d
```

| Dienst | HTTP | HTTPS |
|---|---|---|
| Backend API | `http://localhost:5120` | `https://localhost:7212` |
| Frontend Web | `http://localhost:5072` | `https://localhost:7281` |
| Scalar (API-Doku, nur Development) | `http://localhost:5120/scalar` | — |

**Voraussetzungen (lokal):** .NET 10 SDK, Docker, JDK im `PATH`
(`javac`/`java` werden als Prozess aufgerufen).

Die Datenbank läuft über `docker-compose.yml` (Service `db`, Container
`soopworkshop-db`). In Phase 7 kommen Backend und Frontend als weitere Services dazu.

### `.env` ist in der Entwicklung die eine Wahrheit

Gitignoriert, Vorlage ist `.env.example`. Aus derselben Datei setzt docker-compose die
Datenbank auf **und** das Backend liest seine Konfiguration. Den Connection-String baut
es aus den `POSTGRES_`-Werten, das Passwort steht also nur an einer Stelle. User Secrets
werden nicht mehr gebraucht.

Erstmalige Einrichtung: `.env.example` nach `.env` kopieren, `POSTGRES_PASSWORD` setzen,
`docker compose up -d`, dann Migrationen anwenden (siehe unten).

**Die `.env` wird bewusst als letzte Quelle geladen und schlägt damit auch
Umgebungsvariablen.** Grund: eine vergessene `$env:ConnectionStrings__DefaultConnection`
in der Shell vererbt sich an jedes daraus gestartete Fenster und bewirkt still etwas
anderes, als in der Datei steht — das hat einmal einen Abend gekostet. Ausserhalb von
Development wird sie nicht geladen; im Betrieb gelten echte Umgebungsvariablen.

Welche Werte tatsächlich gelten, steht in der ersten Logzeile des Backends:

```
Konfiguration: Datenbank 127.0.0.1:5432/soopworkshop, Auswertung 10 gleichzeitig, Zeitgrenzen 30s kompilieren / 10s ausfuehren.
```

**`POSTGRES_PASSWORD` in der `.env` geändert?** Dann muss die Datenbank angeglichen
werden — der Wert wirkt nur beim ersten Anlegen des Volumes, danach behält sie ihr altes
Passwort. Der Fehler kommt als `28P01` zurück und sieht aus wie ein Tippfehler, obwohl
beide Seiten für sich stimmen:

```bash
.\scripts\sync-db-password.ps1
```

Setzt das Passwort per `ALTER USER` und prüft anschließend, ob die Anmeldung wirklich
klappt. Ein laufendes Backend muss danach **nicht** neu gestartet werden — der
Connection-String ändert sich nicht, nur das Passwort dahinter. Die Alternative ist
`docker compose down -v` (löscht alle Daten).

**Achtung bei der Fehlersuche:** `psql` **innerhalb** des Containers akzeptiert wegen
`trust` in `pg_hba.conf` jedes Passwort. Eine dort „bestätigte" Übereinstimmung sagt
nichts aus. Verlässlich ist nur eine Verbindung von aussen — das Skript oben macht genau
das über ein kurzlebiges `postgres`-Image gegen `host.docker.internal`.

**`127.0.0.1` statt `localhost`, das ist Absicht.** Unter Windows löst `localhost`
zuerst auf IPv6 `::1` auf. Dort horcht der WSL-Relay von Docker Desktop, reicht die
Verbindung aber nicht zum Container durch — der Fehler kommt als
`28P01 password authentication failed` zurück und sieht damit wie ein falsches
Passwort aus, obwohl er keins ist.

**Auswertung konfigurieren** — in `.env` als `Evaluation__MaxConcurrency` usw. (doppelter
Unterstrich trennt die Ebenen), Standardwerte in `appsettings.json`:

| Schlüssel | Standard | Lokal | Bedeutung |
|---|---|---|---|
| `MaxConcurrency` | 2 | 10 | gleichzeitig ausgewertete Abgaben — begrenzt parallele JVM-Prozesse |
| `CompileTimeoutSeconds` | 30 | 30 | Zeitgrenze für `javac` |
| `RunTimeoutSeconds` | 10 | 10 | Zeitgrenze pro Testfall-Durchlauf |
| `QueueCapacity` | 100 | 100 | Obergrenze der Warteschlange; ist sie voll, wartet das Einreihen |

Die Zeitgrenzen messen **Wanduhrzeit**. Bei hoher Parallelität auf wenigen Kernen
konkurrieren die Prozesse und brauchen länger — dann eher die Grenzen anheben als die
Parallelität senken.

Migrationen — **ohne** `--startup-project`, da `AppDbContextFactory` den Kontext
zur Entwurfszeit selbst baut und die API das Design-Paket nicht referenziert:

```bash
dotnet ef database update --project src/SoopWorkshop.Backend.Infrastructure
```

---

## 4. Architektur

Clean Architecture, 7 Projekte + 1 Testprojekt:

```
SoopWorkshop.Shared                  DTOs, Enums, Constants — von allen referenzierbar
SoopWorkshop.Backend.Domain          Entities, ValueObjects — kennt nur Shared
SoopWorkshop.Backend.Application     Services, Interfaces, Result<T> — kennt Domain + Shared
SoopWorkshop.Backend.Infrastructure  EF Core, Repositories, Java-Checker, ProcessRunner,
                                     Warteschlange + Worker — kennt Application
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

`SubmissionService.CreateAsync` → `IEvaluationQueue` (begrenzter `Channel`) →
`EvaluationWorker` (`BackgroundService`, `MaxConcurrency` parallel) →
`EvaluationService.EvaluateAsync` → `JavaAnalyzer` → Checker → `EvaluationResult`
persistiert → Frontend pollt `/status` und holt bei `Done` das `/result`.

Externe Prozesse (`javac`, `java`, später JUnit) laufen ausschließlich über
`IProcessRunner` — nicht direkt über `Process.Start`.

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
als Teilprüfungen unter Clean Code wandern — siehe §10.

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
- **DTOs**: `Shared/DTOs/<Bereich>/` rein fachlich gegliedert. Lese-DTOs direkt im
  Bereichsordner (`Tasks/TaskItemDto.cs`), Schreib-DTOs unter `<Bereich>/Requests/`
  (`Tasks/Requests/CreateTaskItemDto.cs`).
- **Namensschema Frontend-Seiten**: `Components/Pages/<Bereich>/<Name>.razor`.
- **Nullable + ImplicitUsings** sind überall an — so lassen.
- **Projekt-Tests** spiegeln den Produktivcode als Ordnerbaum
  (`Unit/Infrastructure/Evaluation/Checkers/`). Testklasse heißt `<Klasse>Tests`,
  Testmethode `Methode_Szenario_Erwartung` (z. B.
  `Check_KlasseInCamelCase_LiefertHalbePunkte`). Assertions mit **Shouldly**,
  Mocks mit **NSubstitute**, gleichartige Fälle als `[Theory]` + `[InlineData]`.
  Testet ein Test bewusst eine bekannte Schwäche, hält der Kommentar das als
  **Ist-Verhalten** fest und verweist auf das Finding in §9.

---

## 7. Basis-Smoke-Test vor dem Merge

Gilt für **jede** Phase. Phasenspezifische Schritte kommen jeweils dazu.

1. `.\scripts\stop-dev.ps1`, dann `.\scripts\start-dev.ps1` — Build ohne Warnungen,
   beide Dienste melden „bereit"
2. `dotnet test SoopWorkshop.slnx` — grün
3. Frontend öffnen, Theme dreimal umschalten (Light / Dark / OLED)
4. Aufgabe aus der Sidebar öffnen — Beschreibung, Schwierigkeitsgrad, Tipps sichtbar
5. `.java` hochladen, abgeben, Ergebnisseite abwarten — Punkte und Kategorien erscheinen
6. Browser-Konsole (F12) auf Fehler prüfen
7. Solution zusätzlich in Visual Studio bzw. Rider öffnen und bauen — die
   Kommandozeile deckt IDE-eigene Auflösung von `Directory.Build.props` nicht ab
8. `git status` sauber, `.env` taucht **nicht** auf

**Testdaten:** Ist die Datenbank leer, gibt es nichts zu klicken. Dann zuerst über
`/scalar` oder die Admin-Endpunkte eine sichtbare Kategorie mit sichtbarer Aufgabe
und mindestens einem Testfall anlegen. Sichtbarkeit ist bei beiden nach dem Anlegen
`false` und muss per `PATCH .../visibility` gesetzt werden.

**Hilfsmittel liegen in `tests/manual/`** (eigene README dort):

```bash
.\tests\manual\pruefe-uploads.ps1
```

Prüft die Upload-Validierung und den Status-Endpunkt gegen die laufende API —
inklusive der Fälle, die das Frontend clientseitig blockt und die deshalb im Browser
nicht auslösbar sind. Die Beispielabgaben unter `tests/manual/java/` decken die Fälle
ab, die menschliche Augen brauchen: Zeitüberschreitung, viel Ausgabe auf beiden
Strömen, Laufzeitfehler, Umlaute, Compilerfehler.

## 8. Roadmap

Legende: `[ ]` offen · `[~]` in Arbeit · `[x]` erledigt & geprüft

### Phase 0 — Fundament & Aufräumen ✅
*Abgeschlossen am 2026-08-15, Branch `phase-0-fundament`, 8 Commits.*

- [x] `Directory.Build.props` (TargetFramework, LangVersion, Nullable, ImplicitUsings zentral)
- [x] `Directory.Packages.props` (Central Package Management — Versionen standen 8× dupliziert)
- [x] **Bug:** CORS-Origins hatten vertauschte Schemata; jetzt korrigiert **und** nach
      `Cors:AllowedOrigins` in die Konfiguration verschoben (Vorarbeit Phase 7)
- [x] HttpClient-Registrierung konsolidiert — `AddFrontendServices(apiBaseUrl)` setzt
      die BaseAddress, die Doppelregistrierung in `Frontend.Web/Program.cs` entfällt
- [x] `TaskTestDto` als Lese-DTO ergänzt; `UpdateTaskTestDto` ist wieder reines Schreib-DTO
- [x] DTO-Struktur rein fachlich: `DTOs/Admin/` → `DTOs/Tasks/Requests/`
- [x] Toter Code entfernt: `Score`, `SubmissionFileDto`, `GetVisibleByCategoryAsync`
- [x] Ordner `StateManagment` → `StateManagement`
- [x] Connection-String aus `appsettings.json` → User Secrets bzw. Umgebungsvariable,
      mit klarer Fehlermeldung bei fehlendem Wert; `AppDbContextFactory` liest dieselbe Kette
- [x] MudBlazor-Warnungen behoben — **Build ist warnungsfrei**

### Phase 1 — Projekt-Testfundament (xUnit) ✅
*Nur das Gerüst, keine volle Abdeckung. Absichert die Umbauten in Phase 2–3.*
*Abgeschlossen am 2026-08-15, Branch `phase-1-testfundament`. 25 Tests, grün.*

> **Zur Erinnerung, weil der Begriff doppelt belegt ist:** hier geht es um
> **Projekt-Tests** — C#-Tests, die *unser Programm* prüfen. Die JUnit-Dateien,
> die gegen *die Java-Abgaben* laufen, sind Phase 3. Ein Test wie
> `Check_KlasseInCamelCase_LiefertHalbePunkte` gibt dem Checker einen
> Java-Schnipsel als C#-String und prüft die vergebene Punktzahl — es wird dabei
> nichts kompiliert und kein `javac` gestartet.

- [x] `UnitTest1.cs` entfernt, Ordnerstruktur spiegelt Produktivcode
      (`Helpers/`, `Unit/Application/Common/`, `Unit/Infrastructure/Evaluation/Checkers/`)
- [x] Pakete: NSubstitute 6.2.0, Shouldly 4.3.0 zentral in `Directory.Packages.props`
- [x] Testprojekt referenziert zusätzlich `Shared`, `Backend.API`, `Frontend.Services`
- [x] Erste echte Tests: `CharacterSetChecker` (12), `NamingConventionChecker` (10),
      `Result<T>` (3) — inklusive Tests, die die bekannten Regex-Schwächen als
      **Ist-Verhalten** festschreiben, damit Phase 3 die Änderung sofort sieht
- [x] Benennung festgelegt: `Methode_Szenario_Erwartung` — steht jetzt in §6
- [x] Gegenprobe gemacht: Checker-Logik testweise deaktiviert → 14 von 25 Tests rot,
      danach zurückgenommen

**Bewusst nach Phase 6 verschoben:** bUnit, `Microsoft.AspNetCore.Mvc.Testing` und
die Ordner `Integration/` und `Components/`. Sie werden erst dort gebraucht, ein
`WebApplicationFactory`-Test bräuchte sofort eine Antwort auf die offene
DB-Frage (§10.5), und leere Ordner verfolgt Git ohnehin nicht.
Bei xUnit v2 (2.9.3) geblieben — kein Wechsel auf v3.

### Phase 2 — Backend-Härtung ✅
*Voraussetzung für zuverlässiges Ausführen von JUnit in Phase 3.*
*Abgeschlossen am 2026-08-15, Branch `phase-2-backend-haertung`. 80 Tests, grün.*

- [x] **Neuer Endpunkt `GET /api/submissions/{id}/status`** — liefert `SubmissionStatusDto`
      (Status + `ErrorMessage`); 404 nur noch bei „nicht gefunden". `/result` bleibt
      unverändert und wird vom Frontend erst bei `Done` abgerufen
- [x] Auswertung von `_ = Task.Run(...)` auf `EvaluationWorker` (`BackgroundService`) +
      begrenzte `Channel`-Queue; `Evaluation:MaxConcurrency` begrenzt parallele JVM-Prozesse.
      Beim Start werden verwaiste `Pending`/`Running`-Abgaben auf `Failed` gesetzt
- [x] Prozess-Ausführung hinter `IProcessRunner` abstrahiert (`Infrastructure/Processes/`);
      beide Checker teilen sich jetzt eine Implementierung
- [x] `WaitForExit(int)` → `WaitForExitAsync` + `CancellationToken` durch die ganze Kette
      (`IEvaluationService`, `IJavaAnalyzer`, beide Checker)
- [x] Beide Ausgabeströme werden gleichzeitig gelesen — Deadlock-Risiko behoben, mit
      Gegenprobe in `ProcessRunnerTests` (500 Zeilen auf stdout **und** stderr)
- [x] Upload-Validierung serverseitig (`API/Validation/SubmissionUploadValidator.cs`):
      Endung, Größe, Anzahl, leere Dateien, doppelte Namen, Dateinamen mit Pfadanteilen.
      Grenzen zentral in `Shared/Constants/SubmissionUploadLimits.cs`
- [x] `CleanupWorkingDirectory` fängt zusätzlich `UnauthorizedAccessException`, läuft im
      `finally` und protokolliert statt zu schweigen. Das Arbeitsverzeichnis gehört jetzt
      dem `JavaAnalyzer`, nicht mehr dem `CompilabilityChecker`
- [x] `taskItemId` wird beim Abgeben auf Existenz geprüft → 400 statt 500 aus der
      Fremdschlüsselbedingung
- [x] Strukturiertes Logging (`ILogger`) in allen Application-Services, im Worker,
      im `ProcessRunner` und im `JavaAnalyzer`

**Zusätzlich mitgenommen, weil direkt daran hängend:** Zeichensatz durchgängig auf
UTF-8 statt Systemabhängigkeit — Upload-Lesen, `javac -encoding UTF-8`,
`java -Dstdout.encoding=UTF-8` und die Dekodierung im `ProcessRunner`; javac bekommt
nur noch Dateinamen statt voller Pfade, damit im Feedback `Main.java:3: error` steht
und nicht das Temp-Verzeichnis des Servers; Frontend-Polling auf `/status` umgestellt
inklusive Abbruch nach ~5 Minuten; `SubmissionPollingState` wird injiziert statt
doppelt erzeugt.

> **Wichtig für Phase 3 und 7:** Die JVM setzt `stdout.encoding` unter Windows auf die
> Codepage des Systems (`Cp1252`), **auch wenn die Ausgabe umgeleitet ist** —
> `file.encoding=UTF-8` allein reicht nicht. Jeder neue `java`-Aufruf (auch der
> JUnit-Launcher) braucht deshalb `-Dstdout.encoding=UTF-8 -Dstderr.encoding=UTF-8`.

**Bewusst nicht angefasst, weil Phase 3:** die 65 Gratispunkte bei `tests.Count == 0`,
die Ganzzahl-Division in `TestCaseChecker`, die Regex-Schwächen im
`NamingConventionChecker`. Alle drei sind als **Ist-Verhalten** durch Tests festgehalten.

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
- [ ] Clean Code als Sammelkategorie ausarbeiten (siehe §10, Punkt 1)
- [ ] `DisplayOrder` für Kategorien, `Order` für `TestCaseResult`
- [ ] Admin-Endpunkte für `TaskUnitTestFile` (CRUD)
- [ ] Projekt-Tests für Punkteberechnung und XML-Parsing (reine Funktionen, gut testbar)

### Phase 4 — Frontend Teilnehmer-Sicht

- [ ] `app.css` entrümpeln — enthält noch Bootstrap-Boilerplate aus der Projektvorlage
- [ ] Favicon ergänzen — `wwwroot/` enthält nur `app.css`, `App.razor` verweist auf
      keins; jeder Seitenaufruf erzeugt einen 404 in der Browser-Konsole
- [ ] `NotFound.razor` und `Error.razor` auf MudBlazor umstellen (aktuell rohes HTML)
- [ ] `ThemeService`: Auswahl in `localStorage` persistieren; Service startet mit `Light`,
      `MainLayout` initialisiert aber `Dark` → Flackern beim ersten Render
- [ ] Drawer ist fest `Open="true"` → Toggle im AppBar, Verhalten auf Mobil klären
- [ ] Sidebar: Aufgaben innerhalb einer Kategorie nach `Order` sortieren (fehlt)
- [ ] Zentrale Fehlerbehandlung: `GetFromJsonAsync` wirft bei nicht erreichbarer API
      unbehandelt → Snackbar + Retry statt weißer Seite
- [ ] Lade- und Leerzustände vereinheitlichen (Skeletons statt nackter Spinner)
- [ ] `TaskDetail`: Drag & Drop, Dateiliste mit Entfernen-Button — `MudFileUpload` bringt
      `DragAndDrop`, `SelectedTemplate` und `RemoveFileAsync` bereits mit, nichts davon
      selbst bauen. `MaxFileSize`/`MaximumFileCount`/`Accept` sind seit Phase 2 gesetzt
      und kommen aus `SubmissionUploadLimits`; die Fehlermeldung dazu fehlt noch
- [ ] `SubmissionResult`: `Pending` und `Running` im Text unterscheiden — seit Phase 2 wird
      der Status geliefert, angezeigt wird aber nur „Auswertung laeuft". `Failed` zeigt
      bereits die Fehlermeldung. Offen bleiben „Erneut versuchen" und der Zurück-Link zur
      *richtigen* Aufgabe (geht aktuell nach `/`)
- [ ] `SubmissionResult`: JUnit-Ergebnisse darstellen — Testmethode, Erwartung, Fehlermeldung
- [ ] Sortierte Anzeige der Kategorien und Teilprüfungen (setzt Phase 3 voraus)
- [x] `SubmissionPollingState` sauber verdrahtet (injiziert statt `new`'d, Abbruch nach
      150 Versuchen ≈ 5 Minuten) — in Phase 2 mitgenommen, weil das Status-Polling ohne
      diesen Umbau nicht prüfbar gewesen wäre
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

- [ ] Aus Phase 1 verschoben: Pakete bUnit und `Microsoft.AspNetCore.Mvc.Testing`
      ergänzen, Ordner `Integration/` und `Components/` anlegen. `WebApplicationFactory`
      braucht in `Backend.API/Program.cs` einen `public partial class Program`-Shim
      (Top-Level-Statements) und eine Antwort auf §10.5
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

## 9. Findings-Log

Format: `Datum — Datei — Beschreibung — geplant für Phase X`

- ~~2026-08-15 — `Backend.API/Program.cs` — CORS-Schemata vertauscht~~ — erledigt in Phase 0
- ~~2026-08-15 — `Frontend.Services/DependencyInjection.cs` — doppelte HttpClient-Registrierung~~ — erledigt in Phase 0
- ~~2026-08-15 — `Frontend.Services/StateManagment/` — Ordnername verschrieben~~ — erledigt in Phase 0
- ~~2026-08-15 — `Frontend.Web/.../TaskDetail.razor` — `ActivatorContent` existiert in
  MudBlazor 9.8 nicht; der Upload-Button war kein Activator, die Dateiauswahl liess
  sich darueber nicht oeffnen. War als blosse Build-Warnung fehleingeschaetzt~~ — erledigt in Phase 0
- ~~2026-08-15 — lokale Umgebung — Passwort-Fehlschlag `28P01`: Der Container war neu
  erstellt worden, das Volume stammte vom Vortag. `POSTGRES_PASSWORD` wirkt nur beim
  ersten Initialisieren des Datenverzeichnisses, deshalb galt weiter das alte Passwort.
  Behoben durch Neuaufsetzen per `docker-compose.yml`~~ — erledigt
- ~~2026-08-15 — Datenbank — Schema war dem Code voraus: Migration
  `20260815112648_AddUnitTestingSupport` war angewendet, die Datei existierte weder im
  Repo noch in der Git-Historie. Sie hatte `TestMode`, `TestCode`, `TestFileName`,
  `TemplateCode`, `SolutionCode` auf `TaskItems` sowie `MethodName`, `Points`,
  `IsHidden` auf `TaskTests` angelegt. Ein `migrations add` haette daraus eine
  loeschende Migration erzeugt. Behoben durch Neuaufsetzen der DB auf `InitialCreate`~~
  — erledigt. **Fuer Phase 3 relevant:** dieser verworfene Entwurf legte den JUnit-Code
  direkt auf `TaskItem` statt in eine eigene Entitaet und gab `TaskTest` eigene Punkte.
- ~~2026-08-15 — lokale Umgebung — `28P01` beim Start über `start-dev.ps1`, obwohl User
  Secrets, `appsettings` und Umgebungsvariablen alle korrekt aussahen. Ursache: eine
  Prozess-Umgebungsvariable in der aufrufenden PowerShell vererbte sich an jedes daraus
  gestartete Fenster und schlug die User Secrets. Behoben durch die Umstellung auf `.env`
  als letzte Konfigurationsquelle plus eine Startzeile, die die effektiven Werte nennt~~
  — erledigt. **Lehre:** wenn Konfiguration aus mehreren Quellen kommt, muss der effektive
  Wert protokolliert werden, sonst sucht man im Stacktrace statt in der Konfiguration.
- 2026-08-15 — lokale Umgebung — `psql` **innerhalb** des Containers akzeptiert wegen
  `trust` in `pg_hba.conf` jedes Passwort. Eine damit „bestätigte" Übereinstimmung sagt
  nichts aus; geprüft werden kann nur von aussen
- ~~2026-08-15 — lokale Umgebung — nach einem Neustart schlug die DB-Verbindung mit
  `28P01` fehl, obwohl das Passwort stimmte. `Host=localhost` loest unter Windows
  zuerst auf `::1` auf; dort horcht Dockers WSL-Relay, ohne zum Container
  durchzureichen. Behoben: Connection-String auf `127.0.0.1`, und der Compose-Port
  bindet jetzt explizit `127.0.0.1:5432:5432` statt an alle Schnittstellen~~ — erledigt
- 2026-08-15 — CLAUDE.md dokumentierte `dotnet ef` mit `--startup-project`; das
  schlaegt fehl, da Backend.API `EntityFrameworkCore.Design` nicht referenziert.
  Korrigiert — erledigt
- ~~2026-08-15 — `Application/Submissions/Services/SubmissionService.cs` — Fire-and-Forget-Auswertung ohne Persistenz-Garantie~~ — erledigt in Phase 2
- ~~2026-08-15 — `Frontend.Services/.../SubmissionPollingState.cs` — Endlos-Polling bei `SubmissionStatus.Failed`~~ — erledigt in Phase 2
- ~~2026-08-15 — `Infrastructure/Evaluation/Checkers/*` — jeweils nur ein Ausgabestrom
  gelesen → Deadlock, sobald der Puffer des anderen volläuft~~ — erledigt in Phase 2
- ~~2026-08-15 — `Backend.API/Controllers/SubmissionsController.cs` — `file.FileName`
  ungeprüft in `Path.Combine` mit dem Arbeitsverzeichnis~~ — erledigt in Phase 2
- 2026-08-15 — `Infrastructure/Evaluation/JavaAnalyzer.cs` — schlägt das Speichern des
  Ergebnisses fehl, nachdem `AddAsync` bereits committet hat, bleibt ein Ergebnis mit
  Status `Running` zurück; ein gemeinsamer Transaktionsrahmen fehlt — Phase 6
- 2026-08-15 — `Infrastructure/Evaluation/EvaluationWorker.cs` — Abgaben, die beim
  Herunterfahren abgebrochen werden, bleiben auf `Running` und werden erst beim nächsten
  Start als fehlgeschlagen markiert. Bewusst so: ein sauberes Zurückstellen in die
  Warteschlange braucht Persistenz der Warteschlange — Phase 7
- ~~2026-08-15 — `Infrastructure/Evaluation/EvaluationWorker.cs` — warf das Startup-
  Recovery eine Exception (z. B. DB nicht erreichbar), fuhr .NET den **kompletten Host**
  herunter: `BackgroundServiceExceptionBehavior` steht standardmäßig auf `StopHost`. Die
  API startete dann gar nicht erst, obwohl sie ohne Auswertung noch nützlich wäre~~
  — erledigt in Phase 2. **Merken für Phase 3 und 7:** jeder neue `BackgroundService`
  muss seine Fehler selbst fangen, sonst reißt er den ganzen Server mit.
- 2026-08-15 — `Infrastructure/Evaluation/Checkers/TestCaseChecker.cs` — Aufgaben ohne Testfälle geben 65 Gratispunkte — Phase 3
- 2026-08-15 — `Infrastructure/Evaluation/Checkers/TestCaseChecker.cs` — Ganzzahl-Division verliert Punkte — Phase 3
- 2026-08-15 — `Infrastructure/Evaluation/Checkers/NamingConventionChecker.cs` — Regex prüft auch Strings und Kommentare → False Positives — Phase 3
- 2026-08-15 — `Application/Tasks/Services/TaskCategoryService.cs` — `MapToDto` sortiert Tasks nicht nach `Order` — Phase 4
- 2026-08-15 — `Backend.API/Controllers/Admin/*` — keinerlei Zugriffsschutz — Phase 5

---

## 10. Offene Entscheidungen

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
