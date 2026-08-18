# CLAUDE.md — Arbeits- und Fortschrittsdatei für SoopWorkshop

> Diese Datei ist die **gemeinsame Wahrheit** für die Zusammenarbeit an diesem Projekt.
> Claude liest sie zu Beginn jeder Sitzung und hält die Fortschrittsliste aktuell.
> Stand: 2026-08-18 — Phase 0 bis 6 abgeschlossen. Das Admin-Panel steht (Anmeldung,
> Kategorien und Aufgaben, JUnit-Editor, Bestands-Transfer, Vorschau, Probelauf),
> und die Testabdeckung ist ausgebaut: **386 Projekt-Tests** (davon 84 gegen ein
> echtes PostgreSQL aus dem Container) und **145 Frontend-Tests**. Alle Prüfungen
> laufen mit einem Befehl: `.\scripts\pruefe-alles.ps1`.
> Als Nächstes kommt Phase 7 (Docker Compose, README, Abschluss) — dort auch
> GitHub Actions. Das Frontend heißt **Soop Judge** und ist
> **React 19 + Vite + TypeScript + Tailwind 4** unter
> `src/SoopWorkshop.Frontend/`. Das alte Blazor-Frontend liegt stillgelegt unter
> `archive/`. Der Feinschliff an Farben und Abständen macht der Betreuer von Hand —
> siehe §6.1. Die Abnahme von Phase 6 steht in `tests/manual/abnahme-phase6.md`.

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
Backend und Frontend laufen in je einem eigenen Fenster, damit die Logs lesbar bleiben.
Fehlen die npm-Pakete, installiert das Skript sie einmalig selbst.

Alle Prüfungen in einem Durchgang (Build, Projekt-Tests, Frontend-Build,
Frontend-Tests, Linter) mit Zusammenfassung am Ende:

```bash
.\scripts\pruefe-alles.ps1
```

`-OhneDocker` lässt die Integrationstests aus, `-MitCoverage` schreibt Berichte
nach `artifacts/coverage/`. Details in §7.

Einzeln:

```bash
dotnet build SoopWorkshop.slnx
```

```bash
dotnet test SoopWorkshop.slnx
```

```bash
npm --prefix src/SoopWorkshop.Frontend test
```

```bash
npm --prefix src/SoopWorkshop.Frontend run dev
```

**Die Projekt-Tests brauchen Docker**, seit die Integrationstests gegen ein echtes
PostgreSQL laufen (Testcontainers, §10.5). Ohne Docker geht der schnelle Lauf:

```bash
dotnet test SoopWorkshop.slnx --filter "Category!=Integration"
```

| Dienst | HTTP | HTTPS |
|---|---|---|
| Frontend (Soop Judge) | `http://localhost:5173` | — |
| Backend API | `http://localhost:5120` | `https://localhost:7212` |
| Scalar (API-Doku, nur Development) | `http://localhost:5120/scalar` | — |

**Der Frontend-Port steht an zwei Stellen**: in `vite.config.ts` (`strictPort`) und im
Backend unter `Cors:AllowedOrigins`. Wird er nur an einer geändert, blockt der Browser
jede Anfrage — und der Fehler sieht nach einem kaputten Backend aus.

**Voraussetzungen (lokal):** .NET 10 SDK, **Node.js** (npm), Docker, JDK im `PATH`
(`javac`/`java` werden als Prozess aufgerufen).

**Das Backend hält seine DLLs.** Ein `dotnet build` bei laufendem Backend scheitert mit
`CS2012 … used by another process`. Erst `stop-dev.ps1`, dann bauen.

### Typen aus dem API-Vertrag erzeugen

Nach jeder Änderung an Controllern oder DTOs, bei **laufendem** Backend:

```bash
npm --prefix src/SoopWorkshop.Frontend run api:types
```

Schreibt `src/api/schema.d.ts` aus `/openapi/v1.json`. Die Datei ist eingecheckt, damit
der Build ohne laufendes Backend funktioniert. Fällt im Backend ein Feld weg, bricht
danach die Umsetzung in `src/api/mappers.ts` beim Übersetzen — genau dafür ist sie da.

Die Datenbank läuft über `docker-compose.yml` (Service `db`, Container
`soopworkshop-db`). In Phase 7 kommen Backend und Frontend als weitere Services dazu.

### `.env` ist in der Entwicklung die eine Wahrheit

Gitignoriert, Vorlage ist `.env.example`. Aus derselben Datei setzt docker-compose die
Datenbank auf **und** das Backend liest seine Konfiguration. Den Connection-String baut
es aus den `POSTGRES_`-Werten, das Passwort steht also nur an einer Stelle. User Secrets
werden nicht mehr gebraucht.

Erstmalige Einrichtung: `.env.example` nach `.env` kopieren, `POSTGRES_PASSWORD` **und
`Admin__Password`** setzen, `docker compose up -d`, dann Migrationen anwenden (siehe unten).

**`Admin__Password` ist Pflicht.** Fehlt der Wert, bricht der Start mit einer Meldung ab,
statt still ohne Zugangsschutz zu laufen. Das Passwort schützt `/admin` und alle
`api/admin/*` (§10 Punkt 11).

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
| `JUnitRunTimeoutSeconds` | 30 | 30 | Zeitgrenze für den JUnit-Lauf (deckt alle Testmethoden einer Aufgabe ab) |
| `JUnitJarPath` | `lib/junit-platform-console-standalone-6.1.3.jar` | — | relative Pfade lösen gegen `AppContext.BaseDirectory` auf |
| `CategoryWeights:*` | 15/20/65 | — | Standardgewichte Clean Code / Kompilierbarkeit / Funktionalität (siehe unten) |

**Gewichte sind keine Punkte.** `CategoryWeights` gibt nur das Verhältnis der
Kategorien zueinander an; die erreichbaren Punkte entstehen erst durch die Normierung
auf 100. Nutzt eine Aufgabe eine Kategorie nicht — etwa Funktionalität bei einer
Aufgabe ganz ohne Tests —, fällt sie komplett aus der Wertung und ihr Gewicht verteilt
sich auf die übrigen; aus 15/20/65 wird dann 43/57. Einzelne Aufgaben überschreiben
Gewichte über `TaskCategoryWeight` (Admin-Endpunkt `api/admin/tasks/{id}/weights`).

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

Clean Architecture im Backend, daneben ein eigenständiges Frontend:

```
SoopWorkshop.Shared                  DTOs, Enums, Constants — von allen referenzierbar
SoopWorkshop.Backend.Domain          Entities, ValueObjects — kennt nur Shared
SoopWorkshop.Backend.Application     Services, Interfaces, Result<T> — kennt Domain + Shared
SoopWorkshop.Backend.Infrastructure  EF Core, Repositories, Java-Checker, ProcessRunner,
                                     Warteschlange + Worker, Bestands-Transfer —
                                     kennt Application
SoopWorkshop.Backend.API             Controller, Middleware — kennt Application + Infrastructure
tests/SoopWorkshop.Tests             xUnit (Projekt-Tests)

SoopWorkshop.Frontend                React 19 + Vite + TypeScript + Tailwind 4.
                                     Kein .NET-Projekt, steht deshalb nicht in der .slnx
archive/SoopWorkshop.Frontend.*      stillgelegtes Blazor-Frontend, nicht in der Solution
```

**Abhängigkeitsregeln (nicht verletzen):**

- Domain kennt **kein** EF Core, **keine** Infrastruktur.
- Application definiert Interfaces (`IJavaAnalyzer`, `I*Repository`), Infrastructure implementiert sie.
- Kommunikation Frontend ↔ Backend ausschließlich über HTTP.
- **`Shared` ist nicht mehr der Vertrag.** Das Frontend läuft nicht in .NET und kennt
  die Assembly nicht. Der Vertrag entsteht aus OpenAPI (§3) — was dort nicht
  beschrieben ist, existiert für das Frontend nicht.

**Aufbau des Frontends** (Ordner nach Feature, nicht nach Technik):

```
src/api/         schema.d.ts (erzeugt) · types.ts · mappers.ts · client.ts ·
                 endpoints.ts · uploadLimits.ts · labels.ts
src/components/  AppLayout · Sidebar · CategoryCard · HintPanel · BrandMark ·
                 TaskView · SubmissionForm · ResultView
src/pages/       HomePage · TaskPage · ResultPage · NotFoundPage
src/hooks/       useSubmissionPolling
src/admin/       RequireAdmin · useAdminSession · adminOutlet · validation ·
                 saveState · weights · icons · junitTemplates
  api/           session · catalog · tasks · transfer
  components/    AdminLayout · AdminSidebar · Field · TextInput · TextArea ·
                 NumberInput · Select · Checkbox · formStyles · SaveBar ·
                 ConfirmDialog · IconPickerDialog · OrderButtons ·
                 StringListEditor · ExpectedTypesEditor · TestCaseEditor ·
                 WeightEditor · LineNumberedEditor · UnitTestFileEditor ·
                 TrialRun
  pages/         LoginPage · OverviewPage · CategoriesPage · NewTaskPage ·
                 TaskEditorPage · TaskPreviewPage · TransferPage
```

**Warum zwischen `schema.d.ts` und der Oberfläche noch `mappers.ts` steht:** .NET gibt
im OpenAPI-Dokument kein `required` aus, also ist dort jedes Feld optional, und jedes
`int` kommt als `integer | string` heraus (ASP.NET nimmt beim Binden auch Zahlen als
Zeichenkette an). Ohne Umsetzung stünde in jeder Komponente ein `?? ''`. Der Vertrag
bleibt die Quelle: fällt ein Feld weg, bricht die Umsetzung beim Übersetzen.

**Kernablauf Auswertung:**

`SubmissionService.CreateAsync` → `IEvaluationQueue` (begrenzter `Channel`) →
`EvaluationWorker` (`BackgroundService`, `MaxConcurrency` parallel) →
`EvaluationService.EvaluateAsync` → `JavaAnalyzer` → Checker → `EvaluationResult`
persistiert → Frontend pollt `/status` und holt bei `Done` das `/result`.

Externe Prozesse (`javac`, `java`, später JUnit) laufen ausschließlich über
`IProcessRunner` — nicht direkt über `Process.Start`.

### 4.1 Das Frontend heisst Soop Judge

**React 19 + Vite + TypeScript + Tailwind 4** unter `src/SoopWorkshop.Frontend/`.
Aufbau und Erscheinungsbild stammen aus einem frühen React-Prototyp desselben
Projekts; übernommen sind dessen Tailwind-Klassen weitgehend unverändert.

**Warum nicht mehr Blazor.** Das erste Frontend (Blazor Server + MudBlazor) liegt
stillgelegt unter `archive/`. MudBlazor setzt Material Design um; das damals
angestrebte Bild war in fast jedem Punkt das Gegenteil, und die erste Etappe bestand
zu weiten Teilen darin, die Bibliothek gegen ihre eigenen Annahmen zu biegen.

**Die Lehre ist präziser als „MudBlazor war schuld":** ein Erscheinungsbild, das gegen
die Komponentenbibliothek arbeitet, kostet mehr als es einbringt. Deshalb jetzt
Tailwind **ohne** Komponentenbibliothek — es bringt keine eigene Meinung mit.

**Was der Wechsel am Backend gekostet hat** (alles erledigt, Etappe 4.0):

| Thema | Was nötig war |
|---|---|
| API-Vertrag | 27 Actions von `IActionResult` auf `ActionResult<T>` plus `[ProducesResponseType]`. Vorher enthielt das OpenAPI-Dokument **keine einzige** Antwort |
| Enums | `JsonStringEnumConverter` **an den Enums selbst**, nicht in `AddJsonOptions` — global registriert wirkt er nur zur Laufzeit, der OpenAPI-Erzeuger liest den Typ. Beides lief messbar auseinander |
| Sichtbarkeit | `ToggleVisibility` gab einen anonymen Typ zurück, aus dem kein Schema ableitbar ist → `VisibilityStateDto` |
| Zurück-Link | `TaskItemId` auf `SubmissionStatusDto`, damit die Ergebnisseite zur richtigen Aufgabe zurückführt |
| CORS | auf den Vite-Port 5173 |
| Aufräumen | `EvaluationCategoryNames` gelöscht (toter Code, nur Blazor las es); `Evaluation:CategoryWeights` stand auf den abgeschafften Kategorien `TestCases`/`UnitTests` |

**Was aus dem alten Frontend fachlich weitergelebt hat** (Details in
`archive/README.md`): die Polling-Zustandsmaschine, die Erkenntnis der
unterscheidbaren API-Ausgänge und die Darstellungsregeln aus §5.7.

**Bewusst offen geblieben:** kein Dunkelmodus — der Prototyp kannte nur Hell, und
das Erscheinungsbild wird von Hand nachgezogen (§6.1). Handy-Optimierung ist
aufgeschoben, der Workshop läuft an Laptops. Unterhalb von `lg` klappt die
Seitenleiste als Überlagerung mit Burger-Knopf ein, damit ein schmales Fenster
bedienbar bleibt.

## 5. Bewertungs-Engine (seit Phase 3 umgesetzt)

Der fachliche Kern des Projekts. Alles in diesem Abschnitt beschreibt den **Ist-Stand**.

### 5.1 Zwei Prüfarten, eine Kategorie

Beide Prüfarten beantworten dieselbe Frage — tut das Programm, was die Aufgabe
verlangt — und zahlen deshalb gemeinsam auf die Kategorie **Funktionalität** ein.
Sie unterscheiden sich nur im Aufwand für den Admin. Zwei getrennte Kategorien
nebeneinander waren eine Doppelung in Anzeige und Gewichtung.

Eine Aufgabe kann Konsolen-Testfälle, Aufgaben-Unittests oder beides nutzen:

- **Konsolen-Testfälle** — bestehendes `TaskTest` (Input → erwartete Ausgabe).
  Für frühe Aufgaben, bei denen nur `main` existiert (z. B. „Hallo Soop" ausgeben).
- **Aufgaben-Unittests (JUnit)** — der Admin hinterlegt eine oder mehrere
  JUnit-Testdateien pro Aufgabe. Diese werden zusammen mit der Abgabe kompiliert
  und ausgeführt. Für spätere Aufgaben, bei denen konkrete Methoden und bei
  OOP-Aufgaben mehrere Klassen über mehrere Dateien geprüft werden.

Konsolenausgabe lässt sich auch **innerhalb** von JUnit prüfen (`System.setOut`
umleiten, `Main.main(...)` aufrufen) — genau der Weg für die frühen Aufgaben, in denen
Teilnehmer noch keine eigenen Methoden schreiben. Zwei erprobte Vorlagen liegen unter
`tests/manual/junit/tests/`; die Vorlagen-Bibliothek im Admin-Panel (Phase 5) baut
darauf auf.

**`EvaluationMode` steuert, nicht die Datenlage.** Bei `ConsoleOnly` laufen hinterlegte
JUnit-Dateien nicht, auch wenn welche da sind. Damit fällt eine vergessene Testdatei
auf, statt die Aufgabe still milder zu bewerten. Gegengeprüft wird beim
**Sichtbarschalten**: eine Aufgabe, deren Modus Daten verlangt, die es nicht gibt,
lässt sich nicht sichtbar schalten (`TaskItemService.DescribeMissingTestData`).
Bewusst dort und nicht beim Anlegen — beim Anlegen existieren die Testfälle noch nicht.

### 5.2 Domänen-Objekte

```
TaskItem
  ├─ EvaluationMode          ConsoleOnly | UnitTestOnly | Both
  ├─ ExpectedTypes           → geforderte Klassen, je mit
  │     └─ Methods              ihren erwarteten Methoden: Signatur zur Anzeige,
  │                             daraus abgeleiteter Name zur Prüfung
  ├─ Hints
  ├─ Tests                   → Konsolen-Testfälle
  ├─ UnitTestFiles           → JUnit-Quelldateien: FileName, Content, Order,
  │                            IsVisibleToParticipant (Standard false)
  └─ CategoryWeights         → aufgabenspezifische Gewichte, überschreiben den Standard
```

**Der Aufgaben-Vertrag wird geprüft, nicht nur angezeigt** (`ContractChecker`, läuft
vor dem Kompilieren auf dem bereinigten Quelltext). Grund: Java erzwingt nur, dass
Dateiname und Klassenname zusammenpassen — nicht, dass sie heißen wie gefordert.
Verlangt die Aufgabe `Main` und jemand gibt `Rechner.java` mit `class Rechner` ab,
kompiliert das, die Konsolen-Testfälle laufen durch, und die Abgabe bestand früher
klaglos mit voller Punktzahl.

**Mehrere Klassen, und Methoden gehören zu ihrer Klasse** (seit Phase 5.2). Für die
OOP-Aufgaben am Ende des Workshops hängen mehrere Klassen voneinander ab; der Vertrag
bildet das ab. `JavaTypeBodies.BodyOf` schneidet den Rumpf der geforderten Klasse
heraus, und nur dort wird die Methode gesucht. Vorher lief die Suche über den
gesamten Quelltext: `einzahlen` zählte auch dann als vorhanden, wenn es in `Kunde`
statt in `Konto` stand.

> Das Klammernzählen darin ist nur zulässig, weil vorher
> `JavaSourceText.StripCommentsAndLiterals` läuft — sonst zählte eine geschweifte
> Klammer in einer Zeichenkette mit. **Bekanntes Ist-Verhalten:** eine innere Klasse
> liegt im Rumpf der äußeren, ihre Methoden zählen also auch für diese.

Geprüft wird die **Anwesenheit** der Namen, nicht die vollständige Signatur: die
prüft der Compiler beim Übersetzen der JUnit-Datei ohnehin exakt, und ein Regex über
Java-Quelltext würde daran nur unzuverlässig scheitern. Bekannte Folge davon: ein
bloßer Aufruf `addiere(1, 2)` zählt bereits als Treffer — als Ist-Verhalten getestet.

**Mehrklassige Abgaben funktionieren durchgängig, ohne Einstellung:** bis zu 10
`.java`-Dateien gehen in **einem** `javac`-Aufruf zusammen ins Arbeitsverzeichnis
(`CompilabilityChecker`), Abhängigkeiten zwischen den Klassen lösen sich damit auf.
Der `JUnitChecker` legt die Testdateien daneben und übersetzt mit `-cp <jar>:.` —
eine Testklasse darf also `Konto` und `Kunde` gemeinsam benutzen.

### 5.3 Ausführung der JUnit-Tests

**Version:** `junit-platform-console-standalone-6.1.3.jar`, eingecheckt unter `lib/`
und von `Backend.API.csproj` ins Ausgabeverzeichnis kopiert. Pfad über
`Evaluation:JUnitJarPath`, relative Angaben lösen gegen `AppContext.BaseDirectory` auf.
JUnit 6 setzt Java 17+ voraus (JDK 21 ist eingerichtet).

Ablauf im `JUnitChecker`, alles über `IProcessRunner`:

1. JUnit-Dateien ins Arbeitsverzeichnis schreiben, in dem die Abgabe schon kompiliert ist
2. `javac -encoding UTF-8 -J-Dstdout.encoding=UTF-8 -J-Dstderr.encoding=UTF-8 -cp <jar><Trenner>. <Testdateien>`
3. `java -Dstdout.encoding=UTF-8 -Dstderr.encoding=UTF-8 -jar <jar> execute --class-path .
   --reports-dir junit-reports --disable-banner --disable-ansi-colors --details=none
   --select-class <Klasse>` (je Testdatei ein `--select-class`)
4. XML-Report lesen, **nicht** stdout parsen

**`Path.PathSeparator` statt `;`** im Classpath — unter Linux trennt `:`, und in Phase 7
läuft das im Container.

**Zum Report:** der Launcher schreibt je Engine eine Datei (`TEST-junit-jupiter.xml`,
`TEST-junit-vintage.xml`, …), die meisten davon leer — deshalb werden alle `TEST-*.xml`
gelesen. Ein Rückgabewert ungleich 0 heißt nur „Tests fehlgeschlagen", die Wahrheit
steht im Report.

**`@DisplayName` ist nicht Zierde.** Der Launcher legt ihn in `<system-out>` als Zeile
`display-name: JUnit Jupiter > MainTest > <Text>` ab. Genau dieser Text erscheint beim
Teilnehmer als Beschreibung der Teilprüfung — ohne ihn steht dort der Methodenname.

**Konsolenausgabe aus JUnit heraus prüfen** — drei Regeln, die in den Vorlagen stehen:

1. Der Abfang-Stream braucht `new PrintStream(buffer, true, StandardCharsets.UTF_8)`.
   Ohne die explizite Angabe schreibt er in der Codepage des Systems — dieselbe Falle
   wie bei `stdout.encoding`, nur eine Ebene tiefer.
2. `System.setOut` in `@AfterEach` zurücksetzen.
3. Statische Felder der Abgabe überleben zwischen Testmethoden (eine JVM pro Lauf).

Für Eingaben gilt das **nicht** — die gehören in Konsolen-Testfälle, siehe §5.7.

**Drei Fehlerfälle mit eigener Erklärung**, damit nichts still 0 Punkte ergibt:

- *Testdatei kompiliert nicht gegen die Abgabe* → `JavaCompilerMessages` übersetzt die
  javac-Meldung („cannot find symbol" + `symbol:`/`location:`) in einen Satz, der die
  erwartete Signatur nennt. Die Rohausgabe wird **angehängt**, nicht ersetzt.
- *Kein Report trotz gelaufenem Prozess* → fast immer ein `System.exit(...)` in der
  Abgabe; das beendet die JVM des Testlaufs und reißt alle Testmethoden mit.
- *Zeitüberschreitung* → eigene, großzügigere Grenze `JUnitRunTimeoutSeconds`.

Fehlt das JAR oder verlangt der Modus Unit-Tests ohne hinterlegte Datei, wirft der
Checker — das ist ein Konfigurationsfehler und darf keine Note verändern.

### 5.4 Checker-Pipeline

Interface `IEvaluationChecker` (`Category`, `Order`, `IsApplicable`, `CheckAsync`).
`JavaAnalyzer` bekommt alle Checker injiziert, sortiert nach `Order` und sammelt ihre
Teilprüfungen je Kategorie ein. Eine neue Prüfung wird nur noch in der DI registriert.

**Checker vergeben keine Punkte.** Sie liefern bestandene und nicht bestandene
Teilprüfungen, gerechnet wird ausschließlich im `EvaluationScorer`. Mehrere Checker
dürfen dieselbe Kategorie bedienen — Clean Code entsteht genau so.

**Die wichtigste Regel:** „nicht anwendbar" hängt allein an der *Aufgabendefinition*,
niemals am Ergebnis eines Laufs. Würde eine nicht kompilierende Abgabe ihre Kategorien
verlieren, verteilte sich deren Gewicht auf die übrigen — und kaputter Code bekäme eine
bessere Note als halb funktionierender. Kompiliert die Abgabe nicht, liefern die Checker
durchgefallene Teilprüfungen.

Ausführungsreihenfolge in `EvaluationCheckerOrder`, Anzeigereihenfolge getrennt davon
in `EvaluationCategoryOrder` — kompiliert wird zuerst, angezeigt wird mit Clean Code.

### 5.5 Punktesystem v2

`EvaluationScorer`, reine Funktion ohne Datenbank und Prozesse:

1. Nur anwendbare Kategorien zählen. Ihre Gewichte werden auf 100 normiert; das Gewicht
   weggefallener Kategorien verteilt sich damit von selbst. **Keine Gratispunkte.**
2. Kategoriepunkte = erreichbare Punkte × (bestanden ÷ gesamt), gerechnet in `double`.
3. Gerundet nach **größtem Rest**, Summe exakt 100. Gespeichert wird `int`.
4. Volle Punkte nur, wenn alle Teilprüfungen bestanden sind — Aufrunden ist auf
   `MaxPoints - 1` gedeckelt, sonst stünde 65/65 neben einem roten Testfall.
5. `Passed` je Kategorie = alle Teilprüfungen bestanden.

Standardgewichte in `Evaluation:CategoryWeights` — Clean Code 15, Kompilierbarkeit 20,
Funktionalität 65 —, pro Aufgabe über `TaskCategoryWeight` überschreibbar. Eine Aufgabe
ohne jede Prüfung der Funktionalität wird zu 43/57.

Ein Gewicht ≤ 0 oder eine anwendbare Kategorie ohne Teilprüfung wirft — beides sind
Konfigurationsfehler und dürfen nicht still die Note verschieben.

### 5.6 Clean Code

`EvaluationCategory.CleanCode` ist die **Sammelkategorie**. Teilprüfungen: Zeichensatz,
Klassennamen in PascalCase, kein snake_case. `CharacterSet` und `NamingConventions`
sind keine eigenen Kategorien mehr; die Enum-Werte bleiben als Altlast stehen, weil sie
als `int` in der Datenbank liegen — **nicht wiederverwenden**.

Vor jeder Regex-Prüfung entfernt `JavaSourceText.StripCommentsAndLiterals` Kommentare
sowie String-, Textblock- und Char-Literale. Ohne das schlug die Namensprüfung an,
sobald `mein_wert` in einem Kommentar oder in einer Ausgabe stand.

### 5.7 Wie eine Teilprüfung aussieht

Alle Teilprüfungen einer Auswertung stammen aus fünf Quellen — drei Checkern, den
Konsolen-Testfällen und den `@DisplayName`s der JUnit-Datei. Ohne Absprache klingt
jede anders, und in derselben Kategorie nebeneinander verwirrt das mehr, als es hilft.

**Der Text ist eine Aussage über die Abgabe, das Häkchen sagt, ob sie stimmt.**
Nie eine Feststellung des Ergebnisses — „Kein Umlaut gefunden" mit einem roten Kreuz
behauptet das Gegenteil dessen, was passiert ist.

| Was geprüft wird | Wortlaut |
|---|---|
| Clean Code | „Der Code kommt ohne Umlaute und ohne ß aus" |
| Kompilierbarkeit | „Der Code kompiliert", „Die geforderte Klasse ist vorhanden" |
| Funktionalität, Konsole | „Das Programm addiert zwei positive Zahlen" |
| Funktionalität, JUnit | „Die Methode addiere rechnet auch mit negativen Zahlen" |

Subjekt zuerst: **„Das Programm …"** für alles, was über `main` läuft, **„Die Methode
X …"** für Methodenprüfungen. Konkrete Werte gehören **nicht** in den Text, sondern in
die Zeilen darunter — das gilt auch für lange Signaturen.

**Die Darstellung ist immer dieselbe**, egal aus welcher Quelle:

- `Eingabe` — nur, wenn es eine gab
- `Erwartet` und `Erhalten` — **immer gemeinsam**, sobald eine Erwartung vorliegt.
  Ein „Erwartet" ohne Gegenstück lässt den Leser raten; fehlt eine Seite, steht dort
  ein Gedankenstrich, und der Checker füllt sprechende Werte (`nicht gefunden`,
  `(keine Ausgabe)`) statt ins Leere zu zeigen.
- Prüfungen ohne Vergleich zeigen höchstens eine Meldung (Compilerausgabe, Stacktrace).
- Bestandene Prüfungen zeigen nichts.

**Eingaben gehören in Konsolen-Testfälle, nicht in JUnit.** Eine über `System.setIn`
im Testcode versteckte Eingabe kann die Anzeige nicht kennen — der Teilnehmer sähe
„Erwartet 7", ohne zu erfahren, womit gerechnet wurde. Faustregel: **JUnit prüft
Methoden, Konsolen-Testfälle prüfen das Programm.** Die Ausnahme sind Aufgaben ganz
ohne Eingabe, bei denen JUnit die Ausgabe von `main` prüft (§5.1).

### 5.8 Sortierung der Anzeige

- `EvaluationCategoryOrder` in `Shared`: CleanCode → Kompilierbarkeit → Funktionalität
- `Order` auf `TestCaseResult`, vom Scorer fortlaufend vergeben
- API liefert sortiert aus, das Frontend sortiert zusätzlich — Sortierung ist billig,
  eine wechselnde Anzeige verwirrt

---

## 6. Konventionen

- **Ordner = Feature**, nicht Technik: `Tasks/`, `Submissions/`, `Evaluation/` mit je
  `Interfaces/` und `Services/`.
- **Frontend**: eine Komponente je Datei, benannt wie die Datei. Ordner nach Feature
  (§4). **Bezeichner englisch, Kommentare deutsch** — wie im Backend. In allem, was
  ein Teilnehmer liest, stehen **echte Umlaute** (`ä ö ü ß`); die Ersatzschreibung
  gilt nur für C#-Kommentare.
- **Keine eigene Farbpalette im Frontend.** Die Komponenten benutzen Tailwinds
  Standardfarben (`slate`, `indigo`, `emerald`, `rose`, `amber`) direkt. Der
  Feinschliff passiert von Hand in den Komponenten — eine Token-Zwischenschicht
  stünde dabei nur im Weg (§6.1).
- **Zustände statt Wahrheitswerte.** Ein Ladevorgang hat mehr als zwei Ausgänge:
  `ApiResult` unterscheidet `ok` / `notFound` / `rejected` / `unreachable`, die
  Auswertung `pending` / `running` / `done` / `failed`. Wer das zusammenfasst,
  behauptet irgendwann, eine Aufgabe sei gelöscht, weil der Server nicht läuft.
- **Services geben `Result<T>` zurück**, keine Exceptions für erwartbare Fehlerfälle.
- **DTOs**: `Shared/DTOs/<Bereich>/` rein fachlich gegliedert. Lese-DTOs direkt im
  Bereichsordner (`Tasks/TaskItemDto.cs`), Schreib-DTOs unter `<Bereich>/Requests/`
  (`Tasks/Requests/CreateTaskItemDto.cs`).
- **Nullable + ImplicitUsings** sind überall an — so lassen.
- **Projekt-Tests** spiegeln den Produktivcode als Ordnerbaum
  (`Unit/Infrastructure/Evaluation/Checkers/`). Testklasse heißt `<Klasse>Tests`,
  Testmethode `Methode_Szenario_Erwartung` (z. B.
  `Check_KlasseInCamelCase_LiefertHalbePunkte`). Assertions mit **Shouldly**,
  Mocks mit **NSubstitute**, gleichartige Fälle als `[Theory]` + `[InlineData]`.
  Testet ein Test bewusst eine bekannte Schwäche, hält der Kommentar das als
  **Ist-Verhalten** fest und verweist auf das Finding in §9.

### 6.1 Erscheinungsbild

**Farben und Abstände liegen beim Betreuer.** Das ist bewusst so aufgeteilt: Claude
baut Struktur, Datenanbindung und Korrektheit, der Feinschliff ist subjektiv und
passiert von Hand. Deshalb gibt es **keine Token-Zwischenschicht** — die Komponenten
schreiben Tailwind-Klassen direkt, damit eine Änderung sofort sichtbar ist.

Was unabhängig vom Geschmack gilt:

1. **Akzentfarben nie als Schriftfarbe auf heller Fläche.** Grün und Rot stehen als
   dunkler Text auf getöntem Grund mit Kante (`text-emerald-900` auf `bg-emerald-50`).
   Der Prototyp hatte `text-emerald-600` auf Weiß — gemessen 3,77:1, nötig sind 4,5:1.
2. **Kontraste werden gemessen, nicht geschätzt.** Und zwar richtig:
   - Tailwind 4 liefert Farben als `oklch()`. Wer die ersten drei Zahlen als R/G/B
     liest, bekommt für jedes Paar rund 1:1 und hält ein gesundes Design für kaputt.
     Umrechnen lässt man den Browser über ein 1×1-Canvas.
   - Während Einblend-Animationen misst `getComputedStyle` Zwischenwerte. Die wirksame
     Deckkraft ist das Produkt über alle Vorfahren.
   - **Deaktivierte Bedienelemente sind von WCAG 1.4.3 ausgenommen.** Ein grauer
     `disabled`-Knopf ist kein Befund.
3. **Bewegung liegt in CSS**, nicht in einer Animationsbibliothek. Alle Keyframes enden
   auf dem sichtbaren Zustand und laufen mit `both` — ein Einblenden, das im Fehlerfall
   Inhalt verschluckt, ist schlechter als keins. Ein- und Ausklappen läuft über
   `grid-template-rows: 1fr/0fr`; das braucht keine gemessene Höhe.
4. **Was eingeklappt ist, bekommt `inert`.** Sonst bleiben die Links darin antabbar,
   obwohl niemand sie sieht — eine unsichtbare Tastaturfalle.

> **Zwei Fallen beim Nachmessen im Browser**, beide haben in Phase 4 Zeit gekostet:
>
> - Eine **nicht sichtbare Browser-Ansicht friert jede Animationsuhr ein.**
>   `requestAnimationFrame` liefert null Frames, CSS-Animationen stehen bei
>   `currentTime: 0`, obwohl ihr Zustand „running" heißt. Das sieht exakt aus wie eine
>   tote Animation und ist keine. **Animationen lassen sich so nicht prüfen** — das
>   muss ein Mensch ansehen. Gegenprobe: `document.visibilityState`.
> - `overflow: hidden` beschneidet nur das **Zeichnen**. Ein geklipptes Kind behält
>   seine Layout-Höhe, `getBoundingClientRect()` zeigt sie weiter an. Für „ist das
>   eingeklappt?" misst man den Container, nicht das Kind.
>
> Die ältere Lehre bleibt: **CSS gilt erst als umgesetzt, wenn es nachgemessen ist** —
> aber die Messung selbst braucht genauso viel Misstrauen wie der Code.

---

## 7. Basis-Smoke-Test vor dem Merge

Gilt für **jede** Phase. Phasenspezifische Schritte kommen jeweils dazu.

**Die Schritte 1 bis 3 erledigt seit Phase 6 ein Befehl** — er baut, testet,
baut das Frontend (darin `tsc -b`), lässt die Frontend-Tests laufen und prüft
den Linter, und nennt am Ende jeden Schritt mit Ergebnis und Dauer:

```bash
.\scripts\pruefe-alles.ps1
```

Er bricht **nicht** beim ersten Fehler ab: wer fünf Prüfungen laufen lässt, will
alle fünf Ergebnisse sehen. Läuft das Backend noch, sagt er das vorher, statt in
`CS2012 … used by another process` zu laufen. `-OhneDocker` lässt die
Integrationstests aus, `-MitCoverage` schreibt Berichte nach `artifacts/coverage/`.

1. `.\scripts\stop-dev.ps1`, dann `.\scripts\pruefe-alles.ps1` — alles grün
2. `.\scripts\start-dev.ps1` — Backend und Frontend melden „bereit"
3. `http://localhost:5173` öffnen: Aufgabenliste erscheint, eine Aufgabe anklicken,
   `.java`-Datei abgeben, Ergebnis erscheint nach dem Pollen
4. **Backend stoppen, Seite neu laden** — es muss „Der Server ist nicht erreichbar"
   stehen, **nicht** „Diese Aufgabe gibt es nicht". Danach eine erfundene GUID
   aufrufen: dort muss „gibt es nicht" stehen
5. Eine `.txt` und eine zu große Datei abgeben — die Ablehnung erscheint im Wortlaut
   des Servers, keine Datei verschwindet kommentarlos
6. Solution zusätzlich in Visual Studio bzw. Rider öffnen und bauen — die
   Kommandozeile deckt IDE-eigene Auflösung von `Directory.Build.props` und
   `Directory.Packages.props` nicht ab
7. `git status` sauber; `.env`, `node_modules` und `artifacts/` tauchen **nicht** auf

**Testdaten:** Ist die Datenbank leer, gibt es nichts zu klicken. Beide Skripte laufen
gegen die laufende API und sind unabhängig voneinander mehrfach ausführbar:

```bash
.\tests\manual\seed-phase3.ps1
```

```bash
.\tests\manual\seed-pyramide.ps1
```

```bash
.\tests\manual\seed-oop.ps1
```

Das erste legt drei Aufgaben über alle drei Auswertungsmodi an, das zweite die
Kategorie „Schleifen" mit der Pyramiden-Aufgabe (`UnitTestOnly`, Testdatei für
Teilnehmer sichtbar), das dritte die Kategorie „OOP" mit „Bankkonto" — **zwei
Klassen, die voneinander abhängen**, und ein Vertrag, der jede Methode ihrer
Klasse zuordnet.

**Alle drei melden sich selbst an** (seit Etappe 5.0 verlangt `api/admin/*` das).
Das Passwort kommt aus der `.env`, oder über `-AdminPassword`. Die gemeinsame
Anmeldung steht in `tests/manual/admin-anmeldung.ps1`.

Von Hand geht es auch über `/scalar`: Kategorie und Aufgabe anlegen, Testfälle bzw.
JUnit-Dateien ergänzen, **danach** per `PATCH .../visibility` sichtbar schalten. Die
Reihenfolge ist Pflicht — eine Aufgabe, deren `EvaluationMode` Daten verlangt, die es
noch nicht gibt, lässt sich nicht sichtbar schalten.

**Hilfsmittel liegen in `tests/manual/`** (eigene README dort):

```bash
.\tests\manual\pruefe-uploads.ps1
```

Prüft die Upload-Validierung und den Status-Endpunkt gegen die laufende API —
inklusive der Fälle, die das Frontend clientseitig blockt und die deshalb im Browser
nicht auslösbar sind. Die Beispielabgaben unter `tests/manual/java/` decken die Fälle
ab, die menschliche Augen brauchen: Zeitüberschreitung, viel Ausgabe auf beiden
Strömen, Laufzeitfehler, Umlaute, Compilerfehler.

**Für die Bewertungs-Engine** liegen unter `tests/manual/junit/` die JUnit-Vorlagen und
sieben Beispielabgaben, die zu den Aufgaben aus `seed-phase3.ps1` passen — von der
Musterlösung über den falschen Methodennamen bis zum `System.exit(0)`. Welche Datei
welchen Fall zeigt, listet das Seed-Skript am Ende selbst auf.

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

### Phase 3 — Bewertungs-Engine v2 ✅
*Der fachliche Kern. Details in §5.*
*Abgeschlossen am 2026-08-16, Branch `phase-3-bewertungs-engine`, zwei Etappen plus
Nachtrag 3.1. 221 Tests, grün. Manueller Durchlauf nach §7 bestanden.*

- [x] `EvaluationMode` auf `TaskItem` (ConsoleOnly | UnitTestOnly | Both) — steuert die
      Auswertung, gegengeprüft beim Sichtbarschalten
- [x] Entität `TaskUnitTestFile` + EF-Konfiguration + Migration, inkl.
      `IsVisibleToParticipant` (Standard `false`)
- [x] JUnit-Standalone-JAR eingebunden (6.1.3, `lib/`), Pfad konfigurierbar, CLI-Flags
      und Report-Format empirisch verifiziert und in §5.3 dokumentiert
- [x] `JUnitChecker`: kompilieren mit JUnit im Classpath, ausführen, **XML-Report** parsen;
      `@DisplayName` wird als Beschreibung der Teilprüfung übernommen
- [x] Kompilierfehler der Testdatei in verständliches Teilnehmer-Feedback übersetzt
      (`JavaCompilerMessages`), Rohausgabe bleibt angehängt
- [x] Checker-Pipeline: `IEvaluationChecker` + `EvaluationContext`, `JavaAnalyzer` iteriert
- [x] Punktesystem v2: Gewichte, Gewichtsverteilung bei nicht anwendbaren Kategorien,
      Restverteilung — **behebt die 65 Gratispunkte und die Ganzzahl-Division**
- [x] Clean Code als Sammelkategorie: Zeichensatz und Namenskonventionen sind
      Teilprüfungen, Regex läuft auf bereinigtem Quelltext
- [x] `EvaluationCategoryOrder` für Kategorien, `Order` für `TestCaseResult`
- [x] Admin-Endpunkte für `TaskUnitTestFile` (CRUD + Block-Speicherung) und für die
      aufgabenspezifischen Gewichte
- [x] Projekt-Tests für Punkteberechnung, XML-Parsing und Signatur-Übersetzung

**Zusätzlich mitgenommen:** `tests/manual/seed-phase3.ps1` mit drei Beispielaufgaben
über alle drei Modi, zwei erprobte JUnit-Vorlagen und acht Beispielabgaben unter
`tests/manual/junit/`.

**Nachtrag (Phase 3.1), nach Rückfrage aus dem Review:**

- [x] Konsolen-Testfälle und Unit-Tests zahlen auf **eine** Kategorie „Funktionalität"
      ein. Zwei Kategorien nebeneinander waren eine Doppelung: beide beantworten
      dieselbe Frage, sie unterscheiden sich nur im Aufwand für den Admin
- [x] Der Aufgaben-Vertrag ist strukturiert (`ExpectedClassName`, `ExpectedMethods`)
      und wird vom `ContractChecker` geprüft, statt nur als Freitext dazustehen —
      schließt die Lücke, dass eine Abgabe mit falschem Klassennamen klaglos bestand
- [x] Jede Teilprüfung wird gleich dargestellt und gleich benannt (§5.7). Dafür trägt
      `TestCaseResult` jetzt die `Input`, und die JUnit-Meldung
      `expected: <5> but was: <-1>` wird in dieselben Felder zerlegt
      (`AssertionMessage`) — vorher zeigte ein fehlgeschlagener Unit-Test **gar
      keinen Grund** an, weil die Anzeige an `ExpectedOutput` hing
- [x] Die Eingabesimulation über `System.setIn` ist aus der Vorlage `RechnerTest`
      geflogen: eine im Testcode versteckte Eingabe kann die Anzeige nicht kennen,
      und dieselbe Prüfung leisten die Konsolen-Testfälle sichtbar

**Bewusst anders als geplant:** Die Modus-Validierung greift beim *Sichtbarschalten*
statt beim Speichern. Beim Anlegen einer Aufgabe gibt es die Testfälle noch gar nicht —
eine Prüfung dort hätte das Anlegen jeder JUnit-Aufgabe unmöglich gemacht.

### Phase 4 — Teilnehmer-Frontend „Soop Judge" ✅

*Abgeschlossen am 2026-08-16, Branch `phase-4-frontend-neustart`. React 19 + Vite +
TypeScript + Tailwind 4. 221 Projekt-Tests grün, Build warnungsfrei, Typprüfung und
Linter sauber.*

Der zweite Anlauf nach dem Stilllegen des Blazor-Frontends (§4.1). Das Admin-Panel
bleibt Phase 5 — **im selben Frontend**: die dort geforderte Vorschau „Aufgabe so
anzeigen, wie Teilnehmer sie sehen" ist über zwei Technologiestapel hinweg wertlos.

- [x] **Etappe 4.0 — Backend geöffnet.** API-Vertrag aus OpenAPI erzeugbar, Enums als
      Zeichenkette, `TaskItemId` auf dem Status, CORS, tote Konstanten weg (§4.1)
- [x] **Etappe 4.1 — Gerüst.** Vite + React + TypeScript + Tailwind 4, Schriften als
      npm-Paket statt CDN, eigenes Zeichen als Favicon, `start-dev.ps1` startet beides
- [x] **Etappe 4.2 — Anbindung.** Typen aus dem Vertrag, vier API-Ausgänge,
      Polling-Hook (2 s, Abbruch nach 150 Versuchen)
- [x] **Etappe 4.3 — Die drei Seiten.** Übersicht, Aufgabenseite mit Markdown und
      sichtbarem Vertrag, Ergebnisseite nach §5.7
- [x] **Etappe 4.4 — Querschnitt.** Einklappbare Kategorien, Seitenleiste als
      Überlagerung unterhalb `lg`, Kontraste gemessen, Bewegung in CSS
- [x] **Etappe 4.5 — Abnahme.** Smoke-Test, Testanleitung, diese Datei nachgezogen,
      `FrontRef/` gelöscht

**Bewusst nicht umgesetzt:** kein Dunkelmodus, keine Handy-Optimierung (§4.1). Der
Feinschliff an Farben und Abständen liegt beim Betreuer (§6.1).

<details>
<summary><b>Fachliche Anforderungen — alle erfüllt, hier als Nachweis</b></summary>

Die Liste stand vor dem Neustart hier und beschreibt, *was* der Teilnehmer können
muss. Sie ist unverändert geblieben; nur das *Womit* hat sich geändert.

</details>

<details>
<summary><b>Etappe 4.1 — Designsystem &amp; Fundament ✅ (archiviert)</b></summary>

*Branch `phase-4-1-fundament`. 221 Tests, grün. Build warnungsfrei. Code unter `archive/`.*

- [x] Designsystem nach `DESIGN.md`: Palette in `AppThemes.cs` (Light **und** Dark in
      **einem** `MudTheme`, `IsDarkMode` wählt aus), Nicht-Farb-Token in `app.css`,
      Typo-Skala und Radien-Vokabular gesetzt. Regeln in §6.1
- [x] Border-first: `Elevation="0" Outlined="true"` durchgehend, Hairlines an AppBar
      und Drawer, Seitenbreite 1200px, Buttons ohne Versalien
- [x] Schriften: Google-CDN-Link entfernt (workshop-intern, ggf. ohne Internet), Inter
      als bevorzugte Schrift mit Systemstapel als Rückfall
- [x] `app.css` entrümpelt — von 38 Zeilen Bootstrap-Boilerplate blieb `h1:focus`
- [x] Favicon (`wwwroot/favicon.svg`) — der 404 pro Seitenaufruf ist weg
- [x] `NotFound.razor` und `Error.razor` auf MudBlazor und Deutsch, `Error` mit
      Code-Behind — das war die letzte `@code`-Verletzung im Repo
- [x] `UseStatusCodePagesWithReExecute` wieder aktiviert: `NotFoundPage` in
      `Routes.razor` greift **nur** bei Navigation im laufenden Circuit; ein direkter
      Aufruf endete vorher mit leerer Seite und Status 404 (nachgemessen)
- [x] Theme auf Light/Dark reduziert, `ThemeService` nach
      `Frontend.Services/StateManagement/`, Persistenz über Cookie statt `localStorage` —
      der Server kennt die Wahl schon beim Vorabrendern, damit **kein Flackern**;
      Übergabe an den Circuit über `PersistentComponentState`
- [x] Drawer: `@bind-Open` + `DrawerVariant.Responsive` + Toggle im AppBar
- [x] Sortierung nach `Order` — in `TaskCategoryService.MapToDto` **und** in der Sidebar
- [x] Zentrale Fehlerbehandlung: `ApiResult<T>` trennt Erfolg / nicht vorhanden /
      fehlgeschlagen, `ErrorBoundary` im Layout, Retry in Sidebar und `TaskDetail`,
      Upload-Fehlermeldung des Servers erreicht den Teilnehmer im Wortlaut
- [x] Lade- und Leerzustände: `MudSkeleton` statt nackter Spinner

</details>

**Fachliche Anforderungen — gelten unabhängig vom Werkzeug**

Aufgabenliste und Aufgabenseite:

- [x] Aufgaben nach Kategorie gruppiert, innerhalb der Kategorie nach `Order` sortiert.
      Die API liefert bereits sortiert (seit 4.1), das Frontend sortiert trotzdem selbst
- [ ] Aufgabenübersicht als eigene Seite, nicht nur als Navigationsleiste — **offen**.
      Die Startseite ist derzeit nur eine Begrüßung, die Liste lebt in der Seitenleiste.
      Bei drei Kategorien reicht das; ab einer Handvoll mehr wird eine Übersichtsseite
      nötig
- [x] **Aufgaben-Vertrag sichtbar machen** — `ExpectedClassName`, `ExpectedMethods` und
      die freigeschalteten JUnit-Dateien liefert die API seit Phase 3.1 aus, angezeigt
      wurde nie etwas davon. Der `ContractChecker` bewertet also gegen eine Vorgabe, die
      der Teilnehmer nicht lesen kann. Schließt §10.3
- [x] Schwierigkeitsgrad auf Deutsch (die API liefert das Enum, `DifficultyNames` in
      `Shared/Constants` fehlt noch — oder das Frontend übersetzt selbst)
- [x] Tipps sichtbar, standardmäßig eingeklappt

Abgabe:

- [x] Mehrere `.java`-Dateien, per Auswahl **und** per Drag & Drop, einzeln entfernbar
- [x] Grenzen aus `SubmissionUploadLimits` **clientseitig anzeigen und begründen**:
      `.java`, höchstens 10 Dateien, 1 MB je Datei, 10 MB gesamt. Eine verworfene Datei
      darf nicht kommentarlos verschwinden
- [x] Die Ablehnung des Servers erreicht den Teilnehmer im Wortlaut — die API antwortet
      mit `text/plain` und fertigen deutschen Sätzen („'notiz.txt' ist keine
      .java-Datei."), nicht mit einem Fehlerobjekt

Ergebnis:

- [x] Status pollen: alle 2 s, Obergrenze ~5 Minuten, `Pending` / `Running` / `Done` /
      `Failed` unterscheiden. **`Pending` und `Running` brauchen verschiedene Texte** —
      „in der Warteschlange" ist etwas anderes als „wird gerade geprüft"
- [x] Kategorien in der Reihenfolge aus `EvaluationCategoryOrder`, Teilprüfungen nach
      `Order`
- [x] Teilprüfungen nach den Regeln aus **§5.7** darstellen — Eingabe nur wenn vorhanden,
      Erwartet und Erhalten immer gemeinsam, bestandene Prüfungen zeigen nichts.
      Diese Regeln sind fachlich und frameworkunabhängig
- [x] Compilerausgaben und Stacktraces in Monospace, umbrechend, ohne die Karte zu sprengen
- [x] „Erneut versuchen" und ein Zurück-Link zur **richtigen** Aufgabe. Dafür fehlt
      `TaskItemId` auf `SubmissionStatusDto` — die `Submission`-Entity hat das Feld
      direkt, es braucht kein zusätzliches Laden

Querschnitt:

- [x] **Drei API-Ausgänge unterscheiden**: Erfolg, *gibt es nicht*, *nicht erreichbar*.
      Werden die letzten beiden zusammengeworfen, behauptet die Seite bei gestopptem
      Backend, die Aufgabe sei gelöscht. Genau das ist in 4.1 passiert
- [x] Eine nicht erreichbare API darf nie eine weiße Seite ergeben — Meldung plus
      erneuter Versuch, der Rest der Seite lebt weiter
- [x] Lade- und Leerzustände in der Form dessen, was gleich kommt
- [ ] Hell und Dunkel, Wahl überlebt das Neuladen **ohne Aufblitzen** — **bewusst
      aufgeschoben** (§4.1). Das Gerüst dafür stand kurzzeitig und ist wieder
      ausgebaut worden, statt es halbfertig liegenzulassen: Erscheinungsbild wird von
      Hand nachgezogen, und ein zweiter Satz Farben davor wäre Arbeit auf Verdacht.
      **§10 Punkt 7 ist damit weiter offen** — bei einem reinen Browser-Frontend
      genügt `localStorage` plus ein Inline-Skript im `<head>`, erprobt
- [x] Responsive: **Tablet und Desktop** geprüft (1280 und 768, kein Querscroll,
      Seitenleiste klappt unterhalb `lg` als Überlagerung ein). **Handy bewusst
      aufgeschoben** — der Workshop läuft an Laptops
- [x] Barrierefreiheit: Fokus-Reihenfolge, sichtbarer Fokus, Beschriftungen für
      Bedienelemente ohne Text, Kontraste in beiden Erscheinungsbildern **gemessen**

### Phase 5 — Admin-Panel ✅
*Abgeschlossen am 2026-08-17, Branch `phase-5-admin-panel`, sieben Etappen.
270 Tests, grün. Build warnungsfrei, Typprüfung und Linter sauber.
Abnahme-Anleitung: `tests/manual/abnahme-phase5.md`.*

> **Im selben Frontend wie die Teilnehmer-Sicht** (§10 Punkt 13). Das Gerüst steht:
> React + Vite + TypeScript + Tailwind, API-Client mit vier Ausgängen, erzeugte Typen.
> Die Admin-Endpunkte sind seit Etappe 4.0 im OpenAPI-Vertrag beschrieben, `api:types`
> liefert sie also mit.

- [x] **Etappe 5.0 — Auth.** Passwort aus `Admin__Password`, geprüft gegen
      `POST api/admin/auth/login`, Backend setzt ein **HttpOnly-Cookie**; alle
      `api/admin/*` tragen `[Authorize]`. Kein Token im JavaScript. Fehlt das Passwort,
      **bricht der Start ab** — ein stiller Start ohne Schutz wäre schlimmer. Im Frontend
      hat `ApiResult` dafür einen fünften Ausgang `unauthorized` bekommen; ohne ihn liefe
      ein abgelaufenes Cookie als `rejected` durch und die Anmeldung käme nie. Schließt
      §10 Punkt 11
- [x] **Etappe 5.1 — Gerüst.** Routing unter `/admin`, `AdminLayout` als eigener
      Rahmen mit eigener Seitenleiste (zeigt **auch die verborgenen** Kategorien und
      Aufgaben), Übersichtsseite mit echten Daten, Formularbausteine,
      `admin/validation.ts` mit den gespiegelten DataAnnotations-Grenzen.
      `@tailwindcss/typography` nachinstalliert — die `prose`-Klassen in `TaskPage`
      waren seit Phase 4 wirkungslos. Die Enum-Beschriftungen liegen jetzt in
      `api/labels.ts`, weil Teilnehmersicht und Verwaltung dieselben Wörter brauchen
- [x] **Etappe 5.2a — Vertrag über mehrere Klassen.** `ExpectedClassName` und die
      flache Methodenliste sind zu `TaskItem → TaskExpectedType → TaskExpectedMethod`
      geworden. Der `ContractChecker` sucht die Methode jetzt **im Rumpf ihrer
      Klasse**. Migration zieht den Bestand verlustfrei um (nachgemessen).
      Ende zu Ende belegt: zwei abhängige Klassen abgegeben, `getStand` absichtlich
      in der falschen Klasse — genau diese Teilprüfung fällt durch, die übrigen
      bestehen
- [x] **Etappe 5.2 — Kategorien und Aufgaben bearbeiten.** Sechs Backend-Lücken
      geschlossen (Fremdschlüssel-Prüfungen statt 500, `TaskCategoryId` auf
      `UpdateTaskItemDto`, `CategoryWeights` im Include, `Description` auf die
      wahren 500 Zeichen, Whitelist auf die drei aktiven Kategorien) plus
      **Blockspeicherung für Konsolen-Testfälle** (`PUT .../tests`). Im Frontend
      Kategorienverwaltung, Aufgaben-Editor und das Anlegen. 244 Tests
- [x] Eigener Admin-Bereich unter `/admin` mit eigener Navigation — erledigt in 5.1
- [x] Kategorien: Liste, Anlegen, Bearbeiten, Löschen, Sichtbarkeit umschalten — 5.2.
      Dazu ein **eigenes Symbol je Kategorie** (`IconName`, gewählt aus 135 Symbolen
      in `admin/icons.ts`). Vorher erriet die Seitenleiste es aus dem Namen — beim
      Umbenennen wechselte es damit stillschweigend, und neue Kategorien bekamen nie
      ein eigenes
- [x] Aufgaben: CRUD inkl. Hints, Schwierigkeitsgrad und `EvaluationMode` — 5.2
- [x] Konsolen-Testfälle: CRUD pro Aufgabe — 5.2, als Blockspeicherung
- [x] **Etappe 5.3 — JUnit-Editor, Upload, Vorlagen.** `LineNumberedEditor` mit
      Zeilennummern und Tab-Einrückung, `.java`-Dateien einlesen statt tippen, vier
      Vorlagen. Gespeichert wird über die Blockspeicherung
- [x] **JUnit-Dateien: CRUD pro Aufgabe mit Code-Editor** — 5.3 (monospace +
      Zeilennummern; Monaco bleibt die spätere Ausbaustufe)
- [x] **Vorlagen-Bibliothek** für häufige Testmuster — 5.3: Konsolenausgabe prüfen,
      Rückgabewert prüfen, mehrere Klassen prüfen, Ausnahme erwarten.
      **Bewusst ohne „stdin simulieren"** — das hat Phase 3.1 abgeschafft: eine im
      Testcode versteckte Eingabe kann die Anzeige nicht kennen, Eingaben gehören in
      Konsolen-Testfälle (§5.7)
- [x] **Etappe 5.4 — Import und Export des Bestands.** Der ganze Aufgabenbestand als
      **eine JSON-Datei**: Kategorien, Aufgaben, Vertrag, Tipps, Testfälle,
      JUnit-Dateien und Gewichte. **Ohne Abgaben** — das sind Workshop-Daten, keine
      Konfiguration. Zwei Modi (`Merge`/`Replace`) mit **Vorschau vor dem Ausführen**;
      beim Ersetzen nennt der Dialog die Zahl der Abgaben, die per Cascade mitgehen.
      Die **GUIDs wandern mit**, deshalb verdoppelt ein erneuter Import nichts.
      Aufgeteilt nach dem Vorbild des `EvaluationScorer`: `TaskBundleValidator` und
      `ImportPlanner` sind **reine Funktionen** in Application (ohne Datenbank
      testbar), `TaskTransferService` in Infrastructure führt aus — in der **ersten
      Transaktion des Projekts**. 270 Tests
- [x] Gewichtung der Bewertungskategorien pro Aufgabe einstellbar — 5.2, mit
      Live-Vorschau der Normierung auf 100
- [x] **Etappe 5.5 — Vorschau und Probelauf.** Zwei Umstrukturierungen statt neuer
      Endpunkte: `TaskView` und `ResultView` sind aus `TaskPage` und `ResultPage`
      herausgelöst, `SubmissionForm` ebenfalls. Vorschau und Probelauf benutzen
      **dieselben Komponenten** wie die Teilnehmersicht — eine nachgebaute Vorschau
      liefe beim ersten Umbau auseinander, und man merkte es erst, wenn ein
      Teilnehmer etwas anderes sieht
- [x] Reihenfolge (`Order`) bearbeitbar — 5.2 über Hoch/Runter-Knöpfe. **Bewusst
      kein Drag & Drop:** es löst dasselbe schlechter, weil es mit der Tastatur nicht
      bedienbar ist
- [x] Vorschau: Aufgabe so anzeigen, wie Teilnehmer sie sehen — 5.5. Lädt über
      `GET api/admin/tasks/{id}`, und der liefert **denselben DTO** wie der
      öffentliche Endpunkt, samt derselben Filterung auf freigeschaltete
      JUnit-Dateien. Die Vorschau ist damit ehrlich durch Konstruktion
- [x] **Probelauf:** eigene Musterlösung hochladen und Bewertung prüfen, ohne die
      Aufgabe sichtbar zu schalten — 5.5. Braucht keinen eigenen Endpunkt: die
      Abgabe-Kette prüft die Sichtbarkeit ohnehin nicht. Ein Probelauf erzeugt eine
      **echte** Abgabe; der Hinweis dazu steht in der Karte
- [x] Bestätigungsdialoge vor dem Löschen — 5.2, über das native `<dialog>`
- [x] Formularvalidierung gegen die DataAnnotations — 5.1 in `admin/validation.ts`,
      serverseitig zusätzlich in `TaskBundleValidator` für den Import
- [ ] **Bewusst auf Phase 6 geschoben:** Submissions-Übersicht mit
      Auswertungshistorie. Sie braucht neue Endpunkte und hilft beim Pflegen der
      Aufgaben nicht — der Probelauf deckt den Bedarf des Betreuers ab

### Phase 6 — Projekt-Testabdeckung ✅
*Abgeschlossen am 2026-08-18, Branch `phase-6-testabdeckung`, acht Etappen.
386 Projekt-Tests (302 Unit, 84 Integration) und 145 Frontend-Tests, alle grün.
Build warnungsfrei, Typprüfung und Linter sauber.
Abnahme-Anleitung: `tests/manual/abnahme-phase6.md`.*

> **Diese Phase hat keine Funktionalität geändert.** Am bestehenden Produktivcode
> steht genau eine neue Zeile — `public partial class Program;` als Anker für
> `WebApplicationFactory` — plus ein korrigierter Kommentar. Alles andere ist neu.

**Beim Antreten war mehr erledigt, als die Liste behauptete.** Bereits vor dieser
Phase abgedeckt und hier nur nachgetragen: alle sechs Checker inklusive
`JUnitChecker`, `TaskItemService`, `TaskTestService`, `SubmissionService`,
`EvaluationService`, der `EvaluationScorer` mit Randfällen sowie
`TaskBundleValidator` und `ImportPlanner`.

- [x] **Etappe 6.0 — Die zwei echten Lücken in Application.** `TaskCategoryService`
      und `TaskUnitTestFileService` mit gemockten Repositories. Das **Mapping**
      wird über die öffentlichen Servicemethoden mit abgedeckt, statt die
      `private static`-Methoden nur dem Test zuliebe herauszulösen
- [x] **Etappe 6.1 — Integrationsfundament.** `Microsoft.AspNetCore.Mvc.Testing`,
      `Testcontainers.PostgreSql` und `Respawn` zentral ergänzt, Ordner
      `Integration/` mit `PostgresFixture`, `SoopWorkshopFactory` und
      `IntegrationTestBase`. **Ein** Container je Testlauf, migriert über die
      echten Migrationen, zwischen den Tests räumt Respawn auf. Alle
      Integrationstests tragen `[Trait("Category", "Integration")]`
- [x] **Etappe 6.2 — Repositories gegen echtes PostgreSQL.** Schwerpunkt auf den
      `GetByIdAsync`-Includes („die stillste denkbare Fehlerquelle") und auf der
      Kaskade bis hinunter zur Abgabe — die Zusicherung, auf der der
      `Replace`-Import steht
- [x] **Etappe 6.3 — `TaskTransferService`.** Bis dahin die größte ungetestete
      Klasse (345 Zeilen) und die einzige mit einer Transaktion. Rundlauf,
      idempotentes `Merge`, kaskadierendes `Replace`, Vorschau gleich Ausführung
      und **Rollback**: bricht der Import nach dem Löschen ab, ist die Datenbank
      unverändert. Genau dieser Test wäre gegen EF InMemory grün gewesen, ohne
      irgendetwas zu belegen
- [x] **Etappe 6.4 — Controller über HTTP, risikobasiert.** Nicht Endpunkt für
      Endpunkt, sondern die Zusicherungen, die nur ein echter Aufruf zeigt:
      401 statt 302, Anmelde-Cookie samt `HttpOnly`/`Secure`/`SameSite=None`,
      Ablehnungen als `text/plain` **im Wortlaut**, Upload-Validierung,
      Sichtbarkeits-Sperre, `ExceptionMiddleware` und der CORS-Preflight
      inklusive Gegentest mit fremdem Ursprung
- [x] **Etappe 6.5 — Frontend-Testfundament.** Vitest 4 (Vite 8 verlangt es),
      jsdom, Testing Library. `globals: false` mit ausdrücklichen Importen —
      damit braucht `tsconfig.app.json` keinen neuen `types`-Eintrag und `tsc -b`
      prüft die Tests einfach mit. Dazu die reinen Funktionen: `checkFiles`,
      die **fünf** API-Ausgänge, `mappers`, `validation`, `distributePoints`
- [x] **Etappe 6.6 — Hook und Komponenten.** `useSubmissionPolling` mit Fake
      Timers, die §5.7-Regeln in `CategoryCard`, Sortierung und Schwellen in
      `ResultView`, `SubmissionForm` samt Ablehnungen im Wortlaut, und die
      getrennten Texte für Warteschlange und laufende Prüfung in `ResultPage`
- [x] **Etappe 6.7 — Sammelskript, Coverage, Abnahme.** `scripts/pruefe-alles.ps1`
      führt alle fünf Prüfungen in einem Befehl aus und bricht **nicht** beim
      ersten Fehler ab. Coverage über coverlet und ReportGenerator (als lokales
      Werkzeug in `.config/dotnet-tools.json`) sowie `@vitest/coverage-v8`

**Coverage wird gemessen, nicht erzwungen** (Entscheidung dieser Phase): Backend
89 % Zeilen, Frontend 20 %. Die Frontend-Zahl ist ehrlich, nicht kaputt —
`coverage.include` sorgt dafür, dass ungetestete Dateien mit 0 % in der Zahl
stehen statt aus ihr zu verschwinden. Abgedeckt ist, was in Phase 4 und 5
auffällig war; die Admin-Seiten sind es bewusst nicht.

**Bewusst nicht umgesetzt:**

- [ ] **GitHub Actions — auf Phase 7 verschoben.** Bei genau einem Betreuer
      greift der Hauptnutzen von CI („bricht bei jemand anderem") kaum, und in
      Phase 7 passt es zu den Dockerfiles. Das Sammelskript deckt den Alltag ab
- [ ] **Keine Coverage-Schwelle.** Eine Prozentmarke fängt nichts von dem, was
      dieses Projekt tatsächlich zu Fall gebracht hat (§9), erzeugt aber Arbeit
      an trivialen DTOs
- [ ] **Kein Playwright.** Der Durchlauf aus §7 bleibt der Ende-zu-Ende-Nachweis
- [ ] **Komponententests für die Admin-Seiten.** Sie ändern sich noch und werden
      vom Betreuer bedient, nicht von Teilnehmern
- [ ] Aus Phase 5 weiterhin offen: **Submissions-Übersicht mit
      Auswertungshistorie** — sie braucht neue Endpunkte und ist Feature-Arbeit

### Phase 7 — Docker Compose, README, Abschluss

- [ ] `Dockerfile` Backend — inkl. JDK und JUnit-Standalone-JAR
- [ ] `Dockerfile` Frontend — mehrstufig: Node baut, ein schlanker Webserver liefert
      `dist/` aus. Die Schriften liegen als npm-Paket bei, es wird nichts nachgeladen
- [ ] `docker-compose.yml`: PostgreSQL + Backend + Frontend, Healthchecks, `depends_on`,
      benanntes Volume für die DB
- [ ] Konfiguration vollständig über Umgebungsvariablen (ConnectionString,
      `VITE_API_URL`, Admin-Passwort) — nichts Fest-Verdrahtetes mehr.
      `Cors:AllowedOrigins` muss dort auf den Betriebs-Host zeigen, nicht auf 5173
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

> **Zu den Frontend-Einträgen:** alles mit `Frontend.Web/` oder `Frontend.Services/` im
> Pfad betrifft das stillgelegte Blazor-Frontend (§4.1) und liegt jetzt unter `archive/`.
> Die Einträge bleiben stehen — die **Lehren** darin sind frameworkunabhängig und sollen
> im neuen Frontend nicht noch einmal gelernt werden müssen.

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
- ~~2026-08-15 — `Infrastructure/Evaluation/Checkers/TestCaseChecker.cs` — Aufgaben ohne Testfälle geben 65 Gratispunkte~~ — erledigt in Phase 3
- ~~2026-08-15 — `Infrastructure/Evaluation/Checkers/TestCaseChecker.cs` — Ganzzahl-Division verliert Punkte~~ — erledigt in Phase 3
- ~~2026-08-15 — `Infrastructure/Evaluation/Checkers/NamingConventionChecker.cs` — Regex prüft auch Strings und Kommentare → False Positives~~ — erledigt in Phase 3
- 2026-08-16 — `Frontend.Web/.../SubmissionResult.razor` — der Detailblock hing an
  `!string.IsNullOrEmpty(test.ExpectedOutput)`. JUnit-Ergebnisse hatten dort nichts
  stehen, also zeigte ein fehlgeschlagener Unit-Test überhaupt keinen Grund — obwohl
  die Meldung vorlag. Behoben; die Anzeige prüft jetzt jedes Feld einzeln
- 2026-08-16 — `Infrastructure/Evaluation/JavaAnalyzer.cs` — seit beide Prüfarten auf
  dieselbe Kategorie einzahlen, standen ihre Hinweise aneinandergereiht als ein
  unverständlicher Absatz da. Behoben: gemeinsamer Wortlaut in `EvaluationMessages`
  plus Entdoppelung im Analyzer. **Merken:** wer Kategorien zusammenlegt, muss auch
  ihre Texte zusammenlegen
- 2026-08-16 — `Domain/Entities/TaskItem.cs` — eine Abgabe mit falschem Klassennamen
  bestand klaglos: Java erzwingt nur, dass Dateiname und Klassenname zusammenpassen,
  nicht dass sie heißen wie die Aufgabe verlangt. Bei reinen Konsolenaufgaben fiel das
  nirgends auf. Behoben in Phase 3.1 durch den `ContractChecker`
- 2026-08-16 — `Backend.API/Controllers/Admin/*` — die älteren Admin-Endpunkte verlangen
  nach dem Anlegen einen separaten `PATCH .../visibility` und kennen keine
  Block-Speicherung. Die neuen Endpunkte für JUnit-Dateien und Gewichte sind bereits
  bequemer geschnitten; der Bestand wird beim Bau des Admin-Panels nachgezogen — Phase 5
- 2026-08-16 — `tests/manual/seed-phase3.ps1` — die Aufräumschleife löschte **jede**
  Kategorie statt nur der eigenen. Unter Windows PowerShell 5.1 kommt eine Liste durch
  eine eigene Funktion hindurch als *ein* Objekt an und wird in der Pipeline nicht
  aufgeblättert. `$_.name` liefert dann alle Namen auf einmal, `-eq` filtert das Array
  statt zu vergleichen, und das nicht leere Ergebnis gilt als wahr. Behoben durch eine
  indexbasierte Schleife. **Lehre:** bei löschenden Skripten nie darauf vertrauen, dass
  ein Filter greift — erst mit einem fremden Datensatz gegenprüfen, dass er überlebt
- 2026-08-16 — `tests/manual/seed-phase3.ps1` — `Get-Content` hängt an jeden
  zurückgegebenen String Provider-Eigenschaften (`PSPath`, `PSDrive`, …).
  `ConvertTo-Json -Depth 8` rollt dieses Objekt rekursiv aus: aus 1,8 KB Datei wurden
  105 MB JSON, die der Server als `Request body too large` ablehnte — mit einem Fehler,
  der auf eine völlig unschuldige Stelle zeigte. Behoben durch
  `[System.IO.File]::ReadAllText`. **Lehre:** in PowerShell nie `Get-Content` direkt in
  `ConvertTo-Json` geben
- 2026-08-16 — `Backend.Infrastructure/Persistence/Repositories/SubmissionRepository.cs` —
  was `GetByIdAsync` nicht mitlädt, sieht die Auswertung als „nicht vorhanden" und
  bewertet entsprechend. Beim Ergänzen von `CategoryWeights` und `UnitTestFiles` war das
  jeweils die stillste denkbare Fehlerquelle — bei neuen Navigationen daran denken
- ~~2026-08-15 — `Application/Tasks/Services/TaskCategoryService.cs` — `MapToDto` sortiert Tasks nicht nach `Order`~~ — erledigt in Phase 4.1
- 2026-08-15 — `Backend.API/Controllers/Admin/*` — keinerlei Zugriffsschutz — Phase 5
- 2026-08-16 — `Frontend.Web/Components/Layout/MainLayout.razor.css` — eine isolierte
  CSS-Datei mit `::deep`-Regeln auf `.mud-appbar` war **still wirkungslos**. Die
  CSS-Isolation hängt ihr Scope-Attribut nur an HTML-Elemente, die die Komponente selbst
  rendert; `MainLayout` besteht ausschließlich aus MudBlazor-Komponenten, es gibt dort
  kein Element, an dem `::deep` ansetzen könnte. Nur aufgefallen, weil die berechneten
  Stile im Browser nachgemessen wurden — die Datei sah völlig plausibel aus. Behoben:
  globale Komponenten-Overrides stehen in `app.css`. **Lehre:** CSS gilt erst als
  umgesetzt, wenn `getComputedStyle` es bestätigt
- 2026-08-16 — `Frontend.Web/Program.cs` — `UseStatusCodePagesWithReExecute` war mit der
  Begründung „Sonst not found bei submissions etc." auskommentiert. Nachgemessen: der
  Nebeneffekt tritt nicht (mehr) auf, dafür lieferte **jede** direkt aufgerufene
  unbekannte Adresse eine komplett leere Seite mit Status 404. `NotFoundPage` in
  `Routes.razor` greift nur bei Navigation innerhalb eines laufenden Circuits. Wieder
  aktiviert. **Lehre:** eine auskommentierte Zeile mit Begründung ist eine Behauptung,
  kein Beleg — vor dem Übernehmen nachprüfen
- 2026-08-16 — `Frontend.Services/HttpClients/TaskApiClient.cs` — `null` als einziges
  Fehlersignal warf „gibt es nicht" und „ist nicht erreichbar" zusammen. Bei gestopptem
  Backend stand auf der Aufgabenseite „Diese Aufgabe gibt es nicht (mehr)", obwohl es sie
  gibt — die denkbar irreführendste Auskunft. Behoben durch `ApiResult<T>` mit drei
  Ausgängen. **Merken:** bei jedem neuen API-Aufruf zuerst fragen, wie viele
  unterscheidbare Ausgänge er hat
- 2026-08-16 — `Frontend.Web/Services/AppThemes.cs` — jedes der drei Themes füllte nur
  *eine* der beiden MudBlazor-Paletten. Lief `IsDarkMode` dagegen, kamen kommentarlos die
  MudBlazor-Standardfarben heraus statt eines sichtbaren Fehlers. Behoben: ein Theme mit
  beiden Paletten

**Aus Phase 4 (React-Frontend):**

- 2026-08-16 — `Backend.API/Controllers/*` — der `JsonStringEnumConverter` war zuerst
  global über `AddJsonOptions` registriert. Zur Laufzeit stimmte alles (`"Easy"`), im
  OpenAPI-Dokument stand weiter `type: integer`. **Der Schema-Erzeuger liest den Typ,
  nicht die Registrierung.** Wäre das so geblieben, hätten die erzeugten
  TypeScript-Typen `number` behauptet, während die API Zeichenketten schickt — ein
  Fehler, der erst im Browser auffällt und dort nach einem Backend-Bug aussieht.
  Behoben: Konverter als Attribut an den Enums in `Shared/Enums/`
- 2026-08-16 — `Backend.API/appsettings.json` — `Evaluation:CategoryWeights` stand auf
  `TestCases` und `UnitTests`, also auf Kategorien, die Phase 3 abgeschafft hat. Es
  wirkte nur zufällig richtig, weil `Functionality: 65` still aus dem Standardwert in
  `EvaluationOptions` kam — Konfigurationsbindung *ergänzt* das Wörterbuch, statt es zu
  ersetzen. Wer dort `TestCases` verstellt hätte, hätte nichts bewirkt
- 2026-08-16 — Messen im Browser, drei Fallen in Folge (Details und Gegenmittel in §6.1):
  Tailwind 4 liefert `oklch()` und ein naiver R/G/B-Parser meldet für jedes Paar 1:1;
  `getComputedStyle` misst während Einblendungen Zwischenwerte; **eine nicht gezeichnete
  Browser-Ansicht friert jede Animationsuhr ein**, was exakt wie eine tote Animation
  aussieht. Alle drei haben zu falschen Befunden geführt, einer davon zum unnötigen
  Ausbau einer Bibliothek. **Lehre: die Messung braucht so viel Misstrauen wie der Code,
  und ein Befund wird gegengeprüft, bevor er eine Änderung auslöst**
- 2026-08-16 — `Frontend/src/components/Sidebar.tsx` — der eingeklappte Bereich war
  zuerst nur optisch weg (`overflow: hidden`). Seine Links blieben antabbar: eine
  unsichtbare Tastaturfalle. Behoben mit `inert`. **Merken:** `overflow: hidden`
  beschneidet das Zeichnen, nicht die Bedienbarkeit
- 2026-08-16 — `tests/manual/seed-pyramide.ps1` — Windows PowerShell 5.1 liest eine
  UTF-8-Datei **ohne BOM** als ANSI. Aus `gehört` wäre `gehÃ¶rt` geworden, und
  `UTF8.GetBytes` hätte das anschließend noch einmal kodiert. Beide Seed-Skripte haben
  jetzt eine BOM. Das Senden war schon richtig, es fehlte nur das Lesen

**Aus Phase 5:**

- 2026-08-17 — `Frontend/src/api/client.ts` — der Accept-Kopf lautete
  `application/json, text/plain`. Die API gibt Ablehnungen als nackten String zurück
  (`BadRequest("...")`), und ASP.NET wählt dafür den **ersten passenden** Formatter aus
  dem Accept-Kopf: also JSON. Damit kam jede Server-Ablehnung **JSON-kodiert samt
  Anführungszeichen** an, und der Teilnehmer las
  `"'notiz.txt' ist keine .java-Datei."` statt des Satzes. Genau die Stelle, an der
  CLAUDE.md „im Wortlaut" verspricht. Behoben durch Umdrehen auf
  `text/plain, application/json` — Objekte kommen weiterhin als JSON, für die kann der
  `StringOutputFormatter` nicht einspringen. **Lehre:** Content-Negotiation ist eine
  Reihenfolge, keine Menge; wer einen Text erwartet, muss ihn auch zuerst verlangen
- 2026-08-17 — lokale Umgebung — das Anmelde-Cookie ist `Secure`. **Weder
  `Invoke-WebRequest` noch `curl` senden ein `Secure`-Cookie über `http` zurück**, auch
  nicht an `localhost`. Beide meldeten daraufhin 401 und sahen exakt wie ein kaputter
  Server aus — der aber lieferte mit explizit gesetztem `Cookie`-Kopf brav 200.
  Browser sind hier großzügiger: sie behandeln `localhost` als vertrauenswürdigen
  Ursprung und nehmen das Cookie an, im Browser gemessen. **Lehre:** ein Auth-Fehlschlag
  auf der Kommandozeile beweist nichts über den Browser — und `Invoke-WebRequest`
  verwirft einen von Hand gesetzten `Cookie`-Kopf zusätzlich stillschweigend
- 2026-08-17 — `Frontend/src/index.css` — `@tailwindcss/typography` war nie installiert,
  obwohl `TaskPage` seit Phase 4 `prose prose-slate prose-p:… prose-code:…` benutzt.
  Tailwind kennt `prose` nicht von sich aus: **die ganze Klassenkette war still
  wirkungslos**, die Aufgabenbeschreibung wurde unformatiert gesetzt. Fällt nicht auf,
  weil eine unbekannte Utility-Klasse keinen Fehler erzeugt, sondern nichts tut.
  Behoben mit `@plugin '@tailwindcss/typography'`; nachgemessen am Absatzabstand
  (20 px statt 0 nach dem Preflight-Reset). **Lehre:** dieselbe wie beim `::deep`-Fund
  aus Phase 4 — eine plausibel aussehende Klassenkette ist kein Beleg
- 2026-08-17 — `.env` — beim Bearbeiten mit `Get-Content` + `Set-Content -Encoding utf8`
  wurden die Rahmenzeichen `──` zu `â”€â”€`. Dieselbe Falle wie bei den Seed-Skripten,
  nur andersherum: gelesen wurde UTF-8 ohne BOM als ANSI, geschrieben dann als UTF-8.
  **Lehre:** vor dem Ändern einer Textdatei per PowerShell eine byte-genaue Kopie
  anlegen (`Copy-Item`), nicht auf das Zurückschreiben vertrauen. Umkehrbar ist der
  Schaden über „als UTF-8 lesen, als Windows-1252 kodieren, als UTF-8 dekodieren"
- 2026-08-17 — `Application/Tasks/Services/TaskItemService.cs` — **jedes** Ändern einer
  Aufgabe mit Tipps oder erwarteten Methoden endete in einem 500er. `UpdateAsync`
  leert die Sammlungen und füllt sie mit neuen Entitäten, denen es `Guid.NewGuid()`
  mitgibt. Die Aufgabe ist zu diesem Zeitpunkt aber **bereits von EF verfolgt**, und
  an einem gesetzten Schlüssel erkennt die Änderungsverfolgung eine *bestehende*
  Zeile: sie schickt ein `UPDATE` auf etwas, das es nicht gibt, zählt null betroffene
  Zeilen und wirft `DbUpdateConcurrencyException`. Behoben, indem neue Kinder **ohne
  Id** in die Sammlung wandern. Nie aufgefallen, weil die Seed-Skripte nur anlegen —
  der Aufgaben-Editor aus 5.2 war der erste Aufrufer von `PUT api/admin/tasks/{id}`.
  **Lehre:** beim Anlegen ist ein eigener Guid harmlos, beim Ändern an einem
  verfolgten Graphen ist er eine Falschaussage über die Datenlage
- 2026-08-17 — `Infrastructure/.../TaskItemRepository.cs` — `UpdateAsync` rief
  `Update(item)` auf einer Entität, die aus `GetByIdAsync` kommt und damit schon
  verfolgt ist. Jetzt nur noch bei tatsächlich losgelöster Entität.
  **Die ursprüngliche Begründung dieses Eintrags war falsch — korrigiert in
  Phase 6, siehe den Eintrag von 2026-08-18**
- 2026-08-17 — `Frontend/src/admin/components/ConfirmDialog.tsx` — zwei Fallen beim
  nativen `<dialog>`. Erstens: **React verdrahtet `onCancel` nicht.** Das Ereignis
  blubbert nicht, und Reacts Delegation am Wurzelknoten sieht es deshalb nie — der
  Dialog blieb bei Escape offen, obwohl der Handler plausibel aussah. Zweitens:
  Chrome behandelt Escape für `<dialog>` über den CloseWatcher, und der springt bei
  synthetischen Eingaben nicht an — die Taste kam als `isTrusted: true` an, ein
  `cancel` folgte trotzdem nicht. Behoben mit einem eigenen Listener für **beide**
  Wege. **Lehre:** dieselbe wie in §6.1 — die Messung braucht so viel Misstrauen wie
  der Code, und was sich nicht messen lässt, wird abgesichert statt geglaubt
- 2026-08-17 — `tests/manual/seed-*.ps1` — die Seed-Skripte liefen seit Etappe 5.0
  **gar nicht mehr**: `api/admin/*` verlangt eine Anmeldung, die Skripte kannten
  keine. Beim Absichern der Endpunkte mitgedacht, aber nicht mitgeprüft — und §7
  nennt genau diese Skripte als den Weg zu Testdaten. Behoben mit
  `admin-anmeldung.ps1`. **Lehre:** wer einen Endpunkt absichert, muss auch die
  Werkzeuge nachziehen, die ihn benutzen
- 2026-08-17 — `tests/manual/admin-anmeldung.ps1` — beim Nachziehen zweimal in
  dieselbe Falle getappt, die weiter oben schon steht: das Cookie ist `Secure`
  (der .NET-Speicher schickt es über `http` nicht zurück), **und**
  `Invoke-RestMethod` verwirft einen von Hand gesetzten `Cookie`-Kopf still. Beides
  zusammen ergibt einen 401, der wie ein kaputter Server aussieht. Der Ausweg ist
  ein selbst gebautes `System.Net.Cookie` in einer `WebRequestSession` — das ist
  standardmäßig nicht `Secure` und geht damit auch über `http` hinaus
- 2026-08-17 — `Migrations/…_SplitExpectedContractIntoTypes` — das erzeugte Gerüst
  **hätte den gesamten Vertrag gelöscht**: es wirft `ExpectedClassName` weg und
  benennt `TaskItemId` in `TaskExpectedTypeId` um, ohne die Typen anzulegen — die
  Methoden hätten danach auf nicht existierende Zeilen gezeigt. EF warnt zwar
  („may result in the loss of data"), sagt aber nicht, was genau. Von Hand zu einem
  Umzug umgeschrieben und nachgemessen (5 Methoden vorher, 5 nachher, keine Waisen).
  **Lehre:** eine Migration, die eine Spalte löscht oder umbenennt, wird gelesen
  bevor sie läuft — und vorher wird die Datenbank gesichert
- 2026-08-17 — `Frontend/src/admin/pages/NewTaskPage.tsx` — die Vorauswahl der
  Kategorie stand im Anfangswert von `useState`. Der läuft nur beim ersten Rendern,
  und da lädt das Layout die Kategorien noch: die Auswahl blieb leer, das Anlegen
  scheiterte an der eigenen Prüfung — während im Auswahlfeld sichtbar eine Kategorie
  stand. **Lehre:** ein Anfangswert aus geladenen Daten ist keine Vorauswahl, sondern
  eine Wette auf die Ladereihenfolge

**Aus Phase 6:**

- 2026-08-18 — `Infrastructure/.../TaskItemRepository.cs` — **der Eintrag vom
  2026-08-17 hat den Schaden falsch beschrieben.** Er behauptet, `Update()` auf
  einer verfolgten Entität markiere den ganzen geladenen Graphen und schreibe
  jeden Testfall neu. Nachgemessen (Zustände der Änderungsverfolgung, danach die
  `xmin`-Spalten der Zeilen): EFs Graph-Traversierung hält an bereits verfolgten
  Knoten an, die Kinder bleiben `Unchanged`. Es wird genau **eine** Zeile
  geschrieben, dafür mit allen Spalten statt nur den geänderten. Der beschriebene
  Schaden tritt nur bei einer **losgelösten** Entität ein — und genau dort ist der
  Aufruf nötig. Der Wächter bleibt also richtig, seine Begründung war es nicht;
  Kommentar korrigiert, beide Fälle als Test festgehalten.
  **Lehre:** eine plausible Erklärung für eine richtige Änderung ist trotzdem eine
  Behauptung. Dieselbe Lehre wie beim `::deep`- und beim `prose`-Fund (§6.1) — nur
  diesmal in der eigenen Dokumentation statt im Code
- 2026-08-18 — `Frontend/src/api/uploadLimits.ts` gegen
  `Backend.API/Validation/SubmissionUploadValidator.cs` — die beiden Seiten
  vergleichen Dateinamen **unterschiedlich**: `checkFiles` bitgenau, der
  Validator mit `OrdinalIgnoreCase`. `A.java` und `a.java` kommen im Browser ohne
  Warnung durch und werden erst vom Server abgelehnt. Genau die Doppelpflege, vor
  der der Kopfkommentar in `uploadLimits.ts` warnt. Beide Seiten sind als
  Ist-Verhalten festgehalten; welche Schreibweise gelten soll, ist eine
  Feature-Entscheidung — Phase 7
- 2026-08-18 — `Backend.API/Controllers/Admin/AdminTasksController.cs` —
  `ToggleVisibility` bildet **jeden** Fehlschlag auf `NotFound` ab, auch „die
  Aufgabe hat keine JUnit-Datei". Die Aufgabe gibt es aber. Sichtbar wird das
  heute nicht, weil `TaskEditorPage.onToggleVisibility` die Meldung unabhängig vom
  Ausgang anzeigt — im Frontend käme sie allerdings als `notFound` an, und das ist
  dieselbe Zusammenlegung, gegen die `ApiResult` gebaut wurde (§6). Sauber wäre
  400. Als Ist-Verhalten festgehalten, weil eine Änderung des Statuscodes den
  API-Vertrag betrifft — Phase 7
- 2026-08-18 — `Application/Tasks/Services/TaskUnitTestFileService.cs` —
  `SaveAllAsync` lehnt doppelte `Order`-Werte **nicht** ab, das Gegenstück
  `TaskTestService.SaveAllAsync` dagegen schon. Bei gleichem Wert entscheidet die
  Datenbank über die Anzeigereihenfolge — genau die Begründung, mit der es beim
  Schwesterservice abgelehnt wird. Als Ist-Verhalten festgehalten
- 2026-08-18 — `Frontend/src/admin/weights.ts` — **Verdacht geprüft, kein Befund.**
  Die Restverteilung greift über `candidates[step % candidates.length]` zu, was
  nach einer Doppelvergabe aussieht. Kann nicht eintreten: jeder Eintrag verliert
  beim Abrunden weniger als 1, der Rest ist also stets kleiner als die Zahl der
  Kandidaten. Das Backend rechnet in `EvaluationScorer.LargestRemainder`
  identisch. Steht hier, damit niemand denselben Verdacht ein zweites Mal prüft
- 2026-08-18 — `tests/.../Integration/` — `WebApplicationFactory` braucht vier
  Vorkehrungen, die alle nicht offensichtlich sind und einzeln je einen halben
  Nachmittag kosten: `UseEnvironment("Testing")`, sonst lädt `Program.cs` die
  `.env` des Repositorys **als letzte Quelle** über die Testwerte;
  `ConnectionStrings:DefaultConnection` und `Admin:Password` über `UseSetting`,
  weil `AddInfrastructure` und `AddAdminAuthentication` sonst absichtlich werfen;
  den `EvaluationWorker` entfernen, weil er beim Start verwaiste Abgaben auf
  `Failed` setzt und damit Testdaten ändert; und die Basisadresse auf **https**,
  weil das Anmelde-Cookie `Secure` ist und der `CookieContainer` von .NET es über
  `http` nicht zurückschickt — dieselbe Falle wie bei `Invoke-WebRequest` in
  Phase 5, nur eine Ebene tiefer
- 2026-08-18 — Testcontainers — `Respawn` statt erneutem Migrieren zwischen den
  Tests, und `__EFMigrationsHistory` muss dabei stehen bleiben: wird sie
  mitgeleert, hält EF die Datenbank beim nächsten Zugriff für nicht migriert.
  Migriert wird über die **echten** Migrationen statt über `EnsureCreated` — damit
  prüft jeder Testlauf nebenbei, dass der Migrationsstand durchläuft. In Phase 5
  musste eine Migration von Hand umgeschrieben werden; so etwas fällt sonst erst
  im Betrieb auf
- 2026-08-18 — `Frontend/src/components/SubmissionForm.test.tsx` — `user.upload`
  aus Testing Library filtert die Dateien selbst gegen das `accept`-Attribut des
  Eingabefelds. Eine `.txt` kommt damit gar nicht bei `checkFiles` an, und der
  Test für die Ablehnung lief ins Leere, ohne etwas zu behaupten. Geprüft wird
  jetzt über `fireEvent.drop` — und das ist kein Umweg, sondern der Weg, auf dem
  eine falsche Datei real ankommt: beim Fallenlassen greift `accept` nicht
- 2026-08-18 — `scripts/pruefe-alles.ps1` — dieselbe BOM-Falle wie bei den
  Seed-Skripten, nur andersherum bemerkt: ohne BOM liest Windows PowerShell 5.1
  die UTF-8-Datei als ANSI, und die Rahmenzeichen der Ausgabe kamen als `â”€`
  heraus. Die Skripte unter `scripts/` hatten bisher keine BOM, weil sie ohne
  Sonderzeichen auskamen. **Merken:** sobald ein PowerShell-Skript etwas mit
  Umlauten oder Kästchenzeichen *ausgibt*, braucht die Datei eine BOM
- 2026-08-18 — `Frontend/vite.config.ts` — `@vitest/coverage-v8` zählt ohne
  `coverage.include` nur Dateien, die ein Test importiert hat. Die erste Messung
  meldete 88 % und verschwieg dabei genau das, was fehlt; mit `include` sind es
  ehrliche 20 %. **Lehre:** eine Coverage-Zahl ohne Angabe der Grundgesamtheit
  ist keine Messung, sondern eine Selbstbestätigung

---

## 10. Offene Entscheidungen

1. ~~**Clean-Code-Zuschnitt.**~~ **Entschieden in Phase 3:** `CharacterSet` und
   `NamingConventions` sind Teilprüfungen unter Clean Code, keine eigenen Kategorien
   mehr. Die Enum-Werte bleiben als Altlast stehen (siehe §5.6).
2. ~~**JUnit-Dateien für Teilnehmer sichtbar?**~~ **Entschieden in Phase 3:** pro Datei
   über `IsVisibleToParticipant` schaltbar, Standard `false`. Die öffentliche API liefert
   nur freigeschaltete Dateien aus; die Darstellung im Frontend fehlt noch — Phase 4.
3. ~~**Aufgaben-Vertrag.**~~ **Entschieden in Phase 3.1, erweitert in Phase 5.2:**
   strukturiert als `TaskExpectedType` (geforderte Klasse) mit je eigenen
   `TaskExpectedMethod`, geprüft vom `ContractChecker` als Teilprüfung der
   Kompilierbarkeit. Anzeige beim Teilnehmer und Editor im Panel sind da.
   **Offen geblieben:** die Methode wird im Klassenrumpf gesucht, aber weiterhin nur
   dem Namen nach — Parametertypen prüft allein der Compiler beim Übersetzen der
   JUnit-Datei.
4. **Sandbox-Tiefe.** Docker Compose containerisiert die *Anwendung*, isoliert aber
   nicht die einzelne Abgabe — `javac`/`java` laufen im Backend-Container.
   Für einen workshop-internen Betrieb vertretbar. Echte Isolation pro Abgabe
   (eigener Container je Auswertung) wäre eine spätere Ausbaustufe.
   → Vorschlag: v1 mit Timeouts und Prozesslimits, Hinweis in der README.
5. ~~**Datenbank in Tests.**~~ **Entschieden in Phase 6: Testcontainers.**
   EF InMemory kennt weder Transaktionen noch Cascade-Delete noch
   Fremdschlüsselbedingungen — also genau das, was der Bestands-Transfer aus
   Etappe 5.4 trägt. Der Rollback-Test wäre dagegen grün gewesen, ohne
   irgendetwas zu belegen. Kosten: **ein** Container je Testlauf, rund sechs
   Sekunden; zwischen den Tests räumt Respawn in Millisekunden auf. Wer ohne
   Docker arbeiten will, nimmt `--filter "Category!=Integration"`.
6. ~~**Wie viele Themes?**~~ **Entschieden in Phase 4.1:** Light und Dark, kein OLED.
   Die Entscheidung gilt weiter — OLED unterschied sich nur in drei Grauwerten vom
   Dark-Theme und verdreifachte den Aufwand bei jeder Kontrastprüfung.
7. ~~**Theme-Persistenz.**~~ **Entschieden in Phase 4.1, teilweise überholt:** Cookie
   statt `localStorage`, weil Blazor Server vorab rendert und der Server zu diesem
   Zeitpunkt nicht an `localStorage` herankommt. **Ob das Problem im neuen Frontend
   überhaupt besteht, hängt an der Renderart** — bei einem reinen Browser-Frontend
   reicht `localStorage` plus ein Inline-Skript im `<head>`.

### Aus dem Frontend-Neustart — überwiegend entschieden

8. ~~**Erscheinungsbild.**~~ **Entschieden in Phase 4:** übernommen aus dem
   React-Prototyp, Feinschliff von Hand (§6.1).
9. ~~**Framework oder nur Komponentenbibliothek?**~~ **Entschieden in Phase 4:**
   React 19 + Vite + TypeScript + Tailwind 4, **ohne** Komponentenbibliothek.
10. ~~**API-Vertrag.**~~ **Entschieden in Phase 4:** aus OpenAPI erzeugt
    (`npm run api:types`), `Shared` ist nicht mehr der Vertrag. `JsonStringEnumConverter`
    ist gesetzt. **`ProblemDetails` bewusst nicht** — die Ablehnungen sind fertige
    deutsche Sätze in `text/plain` und werden im Wortlaut angezeigt (§8).
11. ~~**Auth für `api/admin/*`**~~ **Entschieden und umgesetzt in Etappe 5.0:** ein
    Passwort aus `Admin__Password`, einmal gegen `POST api/admin/auth/login` geprüft,
    danach trägt ein **HttpOnly-Cookie** den Zugang — im JavaScript liegt nichts.
    `GET api/admin/auth/session` sagt dem Frontend beim Start, ob eine Anmeldung nötig
    ist. Keine Benutzerverwaltung: der Workshop hat genau einen Betreuer.
    **Offen bleibt für Phase 7:** das Cookie ist `Secure`, das trägt im Betrieb nur
    über HTTPS.
12. ~~**Werkzeuge.**~~ **Erledigt:** Context7 liefert aktuelle Bibliotheks-Doku
    (`.mcp.json`), der Browser kann messen und Screenshots machen, sobald die Anwendung
    aus dem Projekt heraus läuft (`.claude/launch.json`). **Eine Einschränkung bleibt:
    Animationen lassen sich so nicht prüfen** — eine nicht sichtbare Browser-Ansicht
    friert jede Animationsuhr ein (§6.1). Das muss ein Mensch ansehen.
13. ~~**Phasenschnitt.**~~ **Entschieden in Phase 4:** Teilnehmer-Sicht ist Phase 4,
    Admin-Panel Phase 5 — beide **im selben Frontend**. Die Vorschau aus Phase 5
    („Aufgabe so anzeigen, wie Teilnehmer sie sehen") wäre über zwei Stapel wertlos.

### Neu offen nach Phase 4

14. **Übersichtsseite für Aufgaben.** Die Liste lebt derzeit nur in der Seitenleiste.
    Bei drei Kategorien reicht das; wächst der Bestand, braucht es eine eigene Seite.
15. ~~**Frontend-Tests.**~~ **Entschieden und umgesetzt in Phase 6:** Vitest 4
    (Vite 8 verlangt diesen Major) + Testing Library + jsdom, Konfiguration im
    `test`-Block von `vite.config.ts`. `globals: false` mit ausdrücklichen
    Importen, damit `tsconfig.app.json` unangetastet bleibt und `tsc -b` die
    Tests mitprüft. 145 Tests; abgedeckt ist, was in Phase 4 und 5 auffällig
    war, die Admin-Seiten bewusst nicht.
