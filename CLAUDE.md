# CLAUDE.md — Arbeits- und Fortschrittsdatei für SoopWorkshop

> Diese Datei ist die **gemeinsame Wahrheit** für die Zusammenarbeit an diesem Projekt.
> Claude liest sie zu Beginn jeder Sitzung und hält die Fortschrittsliste aktuell.
> Stand: 2026-08-16 — Phase 0 bis 3 abgeschlossen. **Phase 4 ist neu aufgesetzt:** das
> Blazor-Frontend ist stillgelegt und liegt unter `archive/`, ein neues ist noch nicht
> gewählt. Lies **§4.1** und **§10 ab Punkt 8**, bevor du am Frontend planst.
> Das Backend ist davon unberührt und vollständig.

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

Alles starten (Datenbank, Build, Backend — ein Frontend gibt es derzeit nicht, §4.1):

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
Das Backend läuft in einem eigenen Fenster, damit die Logs lesbar bleiben.

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
| Scalar (API-Doku, nur Development) | `http://localhost:5120/scalar` | — |
| ~~Frontend Web~~ | ~~`http://localhost:5072`~~ | ~~`https://localhost:7281`~~ — stillgelegt, §4.1 |

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

Clean Architecture, 7 Projekte + 1 Testprojekt:

```
SoopWorkshop.Shared                  DTOs, Enums, Constants — von allen referenzierbar
SoopWorkshop.Backend.Domain          Entities, ValueObjects — kennt nur Shared
SoopWorkshop.Backend.Application     Services, Interfaces, Result<T> — kennt Domain + Shared
SoopWorkshop.Backend.Infrastructure  EF Core, Repositories, Java-Checker, ProcessRunner,
                                     Warteschlange + Worker — kennt Application
SoopWorkshop.Backend.API             Controller, Middleware — kennt Application + Infrastructure
tests/SoopWorkshop.Tests             xUnit (Projekt-Tests)

archive/SoopWorkshop.Frontend.*      stillgelegtes Blazor-Frontend, nicht in der Solution
```

**Ein Frontend gibt es derzeit nicht** — siehe §4.1. Das Backend ist davon unberührt.

**Abhängigkeitsregeln (nicht verletzen):**

- Domain kennt **kein** EF Core, **keine** Infrastruktur.
- Application definiert Interfaces (`IJavaAnalyzer`, `I*Repository`), Infrastructure implementiert sie.
- Kommunikation Frontend ↔ Backend ausschließlich über HTTP. Solange das Frontend in .NET
  lief, waren das direkt die DTOs aus `Shared`; ein Frontend ausserhalb von .NET braucht
  stattdessen einen erzeugten Vertrag — siehe §4.1.

**Kernablauf Auswertung:**

`SubmissionService.CreateAsync` → `IEvaluationQueue` (begrenzter `Channel`) →
`EvaluationWorker` (`BackgroundService`, `MaxConcurrency` parallel) →
`EvaluationService.EvaluateAsync` → `JavaAnalyzer` → Checker → `EvaluationResult`
persistiert → Frontend pollt `/status` und holt bei `Done` das `/result`.

Externe Prozesse (`javac`, `java`, später JUnit) laufen ausschließlich über
`IProcessRunner` — nicht direkt über `Process.Start`.

### 4.1 Frontend-Neustart (Stand 2026-08-16)

**Das Blazor-Frontend ist stillgelegt.** Es liegt vollständig unter `archive/`, ist aus
`SoopWorkshop.slnx` genommen und wird nicht gebaut. Begründung und Anleitung zum
Reaktivieren stehen in `archive/README.md`. `DESIGN.md` ist entfernt — das dort
beschriebene Erscheinungsbild war auf MudBlazor gemünzt und wird neu entschieden.

Anlass: das Ergebnis hat optisch nicht überzeugt. Der wahrscheinliche Grund ist, dass
MudBlazor Material Design umsetzt (Schatten, großzügige Abstände, Farbrollen), während
das angestrebte Bild dessen Gegenteil war (1px-Kanten, kompakte Dichte, ein Akzent).
Etappe 4.1 bestand zu weiten Teilen darin, die Bibliothek gegen ihre eigenen Annahmen zu
biegen.

**Was der Neustart am Backend berührt** — nichts davon ist erledigt, alles gehört in die
neue Planung:

| Thema | Warum es auffällt, sobald das Frontend nicht mehr .NET ist |
|---|---|
| **Enums gehen als Zahl über die Leitung** | Kein `JsonStringEnumConverter` registriert (`Backend.API/Program.cs`). Für Blazor egal, weil es dieselbe `Shared`-Assembly nutzte. Ein anderes Frontend liest dann `difficulty: 0` und muss die Bedeutung raten |
| **Kein API-Vertrag** | `Shared` war der Vertrag. Ohne .NET-Frontend braucht es einen erzeugten (OpenAPI → Typen). `Microsoft.AspNetCore.OpenApi` und Scalar sind bereits eingebunden |
| **Fehlerantworten sind Klartext-Strings**, kein `ProblemDetails` | Auswertbar ist das nur mit Konventionswissen |
| **CORS** | `Cors:AllowedOrigins` steht auf den alten Frontend-Ports 5072/7281 |
| **Auth für `api/admin/*`** (Phase 5) | Der geplante Weg — statisches Token, das dank Blazor Server den Browser nie verlässt — funktioniert **nur** mit einem serverseitig gerenderten Frontend. Bei einem Browser-Frontend liegt das Token im Browser. Das ist eine echte Neuentscheidung, keine Portierung |
| **Phase 7** | Ein drittes Image plus Node-Toolchain im Build |

**Was aus dem alten Frontend fachlich weiterlebt** (Details in `archive/README.md`):
die Polling-Zustandsmaschine, die Erkenntnis der drei API-Ausgänge (Erfolg / gibt es
nicht / nicht erreichbar) und die Darstellungsregeln für Teilprüfungen aus §5.7.

---

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
  ├─ ExpectedClassName       wie die Klasse heißen muss (nullable)
  ├─ ExpectedMethods         → erwartete Methoden: Signatur zur Anzeige,
  │                            daraus abgeleiteter Name zur Prüfung
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

Geprüft wird die **Anwesenheit** der Namen, nicht die vollständige Signatur: die
prüft der Compiler beim Übersetzen der JUnit-Datei ohnehin exakt, und ein Regex über
Java-Quelltext würde daran nur unzuverlässig scheitern. Bekannte Folge davon: ein
bloßer Aufruf `addiere(1, 2)` zählt bereits als Treffer — als Ist-Verhalten getestet.

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
- **Frontend-Konventionen stehen offen.** Die alten Regeln (Code-Behind statt
  `@code`-Blöcke, `X.razor.css`, `Components/Pages/<Bereich>/<Name>.razor`) galten für
  Blazor und sind mit dem Neustart hinfällig — siehe §4.1. Neue Regeln kommen mit der
  Entscheidung für das neue Frontend hierher. Der *Geist* der alten bleibt gültig:
  Auszeichnung und Logik getrennt, Styles nah an der Komponente, Ordner nach Feature.
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

### 6.1 Designsystem — offen

**Es gibt derzeit kein Designsystem.** `DESIGN.md` ist mit dem Frontend-Neustart entfernt
worden (§4.1); es beschrieb ein Erscheinungsbild, das gegen MudBlazor durchgesetzt werden
musste. Was an seine Stelle tritt, wird zusammen mit dem Frontend entschieden.

Vier Regeln haben sich unabhängig vom Werkzeug bewährt und sollten in das neue System
übernommen werden:

1. **Eine Wahrheit pro Token-Art.** Farben an genau einer Stelle, alles Übrige an genau
   einer anderen. Zwei Farblisten laufen still auseinander.
2. **Eigenes CSS greift nur auf Variablen zu, nie auf Hex-Werte.**
3. **Akzentfarben nie als Schriftfarbe** — nur Icon, Kante oder getönter Hintergrund.
   `#16a34a` auf Weiß liegt bei ~3,0:1 und reißt die Schwelle von 4,5:1. Die Aussage
   trägt der Text, die Farbe verstärkt sie nur.
4. **Ein Erscheinungsbild, das gegen die Komponentenbibliothek arbeitet, kostet mehr, als
   es einbringt.** Das ist die Lehre aus Etappe 4.1 — erst das Aussehen festlegen, dann
   eine Bibliothek wählen, die es *von sich aus* kann.

> **Und eine Falle, die frameworkspezifisch war, aber typisch ist:** eine
> `MainLayout.razor.css` mit `::deep`-Regeln war **still wirkungslos**, weil Blazors
> CSS-Isolation ihr Scope-Attribut nur an HTML-Elemente hängt, die die Komponente selbst
> rendert — und das Layout bestand nur aus Fremdkomponenten. Aufgefallen ist es erst über
> `getComputedStyle` im Browser. **CSS gilt erst als umgesetzt, wenn es nachgemessen ist.**

---

## 7. Basis-Smoke-Test vor dem Merge

Gilt für **jede** Phase. Phasenspezifische Schritte kommen jeweils dazu.

> **Solange es kein Frontend gibt**, entfallen die Schritte 3 bis 6. Der fachliche
> Durchlauf läuft stattdessen über `/scalar`: Abgabe hochladen, `/status` pollen, bei
> `Done` das `/result` holen. Sobald das neue Frontend steht, werden die Schritte
> ersetzt — nicht wieder eingefügt, sie waren auf Blazor gemünzt.

1. `.\scripts\stop-dev.ps1`, dann `.\scripts\start-dev.ps1` — Build ohne Warnungen,
   das Backend meldet „bereit"
2. `dotnet test SoopWorkshop.slnx` — grün
3. *(Frontend — siehe Hinweis oben)*
4. *(Frontend)*
5. *(Frontend)*
6. *(Frontend)*
7. Solution zusätzlich in Visual Studio bzw. Rider öffnen und bauen — die
   Kommandozeile deckt IDE-eigene Auflösung von `Directory.Build.props` nicht ab
8. `git status` sauber, `.env` taucht **nicht** auf

**Testdaten:** Ist die Datenbank leer, gibt es nichts zu klicken. Das erledigt bei
laufender API ein Aufruf — er legt drei Beispielaufgaben über alle drei Auswertungsmodi
an und schaltet sie sichtbar:

```bash
.\tests\manual\seed-phase3.ps1
```

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

### Phase 4 — Frontend (Neustart) 🔄

**Die Phase ist am 2026-08-16 neu aufgesetzt worden.** Das Blazor-Frontend ist
stillgelegt (§4.1), `DESIGN.md` ist entfernt. Alles unter „Etappe 4.1" ist **erledigt,
aber archiviert** — es steht hier als Beleg, was schon einmal gelöst war und in welcher
Form es wiederkommen muss.

**Die Neuplanung passiert in einem eigenen Chat** und beantwortet in dieser Reihenfolge:

1. **Wie soll es aussehen?** Erst das Erscheinungsbild, dann das Werkzeug — die Lehre
   aus 4.1 (§6.1).
2. **Womit?** Anderes Framework (React/…) oder andere Komponentenbibliothek unter
   Blazor. Der Aufwandsunterschied ist groß: ein Bibliothekswechsel lässt Backend,
   `Shared` als Vertrag und die Auth-Planung unberührt, ein Frameworkwechsel nicht
   (Tabelle in §4.1).
3. **Welche Werkzeuge braucht die Umsetzung?** Erweitertes Skillset über MCP-Server,
   Komponenten-Vorschau, Screenshot-gestützte Prüfung. In dieser Sitzung war der
   Screenshot-Kanal nicht verfügbar — geprüft werden konnte nur über `getComputedStyle`.
4. **Was davon ist Phase 4, was Phase 5?** Teilnehmer-Sicht und Admin-Panel teilen sich
   dasselbe Frontend; die Trennung der beiden Phasen war auf Blazor gemünzt.

Die fachlichen Anforderungen darunter gelten unverändert — sie beschreiben, *was* der
Teilnehmer können muss, nicht *womit*.

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

- [ ] Aufgaben nach Kategorie gruppiert, innerhalb der Kategorie nach `Order` sortiert.
      Die API liefert bereits sortiert (seit 4.1), das Frontend sortiert trotzdem selbst
- [ ] Aufgabenübersicht als eigene Seite, nicht nur als Navigationsleiste
- [ ] **Aufgaben-Vertrag sichtbar machen** — `ExpectedClassName`, `ExpectedMethods` und
      die freigeschalteten JUnit-Dateien liefert die API seit Phase 3.1 aus, angezeigt
      wurde nie etwas davon. Der `ContractChecker` bewertet also gegen eine Vorgabe, die
      der Teilnehmer nicht lesen kann. Schließt §10.3
- [ ] Schwierigkeitsgrad auf Deutsch (die API liefert das Enum, `DifficultyNames` in
      `Shared/Constants` fehlt noch — oder das Frontend übersetzt selbst)
- [ ] Tipps sichtbar, standardmäßig eingeklappt

Abgabe:

- [ ] Mehrere `.java`-Dateien, per Auswahl **und** per Drag & Drop, einzeln entfernbar
- [ ] Grenzen aus `SubmissionUploadLimits` **clientseitig anzeigen und begründen**:
      `.java`, höchstens 10 Dateien, 1 MB je Datei, 10 MB gesamt. Eine verworfene Datei
      darf nicht kommentarlos verschwinden
- [ ] Die Ablehnung des Servers erreicht den Teilnehmer im Wortlaut — die API antwortet
      mit `text/plain` und fertigen deutschen Sätzen („'notiz.txt' ist keine
      .java-Datei."), nicht mit einem Fehlerobjekt

Ergebnis:

- [ ] Status pollen: alle 2 s, Obergrenze ~5 Minuten, `Pending` / `Running` / `Done` /
      `Failed` unterscheiden. **`Pending` und `Running` brauchen verschiedene Texte** —
      „in der Warteschlange" ist etwas anderes als „wird gerade geprüft"
- [ ] Kategorien in der Reihenfolge aus `EvaluationCategoryOrder`, Teilprüfungen nach
      `Order`
- [ ] Teilprüfungen nach den Regeln aus **§5.7** darstellen — Eingabe nur wenn vorhanden,
      Erwartet und Erhalten immer gemeinsam, bestandene Prüfungen zeigen nichts.
      Diese Regeln sind fachlich und frameworkunabhängig
- [ ] Compilerausgaben und Stacktraces in Monospace, umbrechend, ohne die Karte zu sprengen
- [ ] „Erneut versuchen" und ein Zurück-Link zur **richtigen** Aufgabe. Dafür fehlt
      `TaskItemId` auf `SubmissionStatusDto` — die `Submission`-Entity hat das Feld
      direkt, es braucht kein zusätzliches Laden

Querschnitt:

- [ ] **Drei API-Ausgänge unterscheiden**: Erfolg, *gibt es nicht*, *nicht erreichbar*.
      Werden die letzten beiden zusammengeworfen, behauptet die Seite bei gestopptem
      Backend, die Aufgabe sei gelöscht. Genau das ist in 4.1 passiert
- [ ] Eine nicht erreichbare API darf nie eine weiße Seite ergeben — Meldung plus
      erneuter Versuch, der Rest der Seite lebt weiter
- [ ] Lade- und Leerzustände in der Form dessen, was gleich kommt
- [ ] Hell und Dunkel, Wahl überlebt das Neuladen **ohne Aufblitzen**
- [ ] Responsive: Mobil, Tablet, Desktop
- [ ] Barrierefreiheit: Fokus-Reihenfolge, sichtbarer Fokus, Beschriftungen für
      Bedienelemente ohne Text, Kontraste in beiden Erscheinungsbildern **gemessen**

### Phase 5 — Admin-Panel

> **Hängt am Frontend-Neustart (§4.1).** Die Punkte unten beschreiben weiter das
> fachlich Nötige, aber die Auth-Frage ist **neu zu entscheiden**, und ob Admin-Panel und
> Teilnehmer-Sicht getrennte Phasen bleiben, ebenfalls.

- [ ] **Auth — neu zu entscheiden.** Der bisherige Plan (festes Passwort aus der
      Konfiguration, API prüft ein statisches Token, das dank Blazor Server den Browser
      nie verlässt) funktioniert **nur** mit einem serverseitig gerenderten Frontend.
      Läuft das Frontend im Browser, liegt das Token dort — dann braucht es einen anderen
      Weg. `api/admin/*` ist bis dahin **komplett offen**.
- [ ] Eigener Admin-Bereich unter `/admin` mit eigener Navigation
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

- [ ] Aus Phase 1 verschoben: `Microsoft.AspNetCore.Mvc.Testing` ergänzen, Ordner
      `Integration/` anlegen. `WebApplicationFactory` braucht in `Backend.API/Program.cs`
      einen `public partial class Program`-Shim (Top-Level-Statements) und eine Antwort
      auf §10.5
- [ ] **bUnit ist hinfällig, solange kein Blazor-Frontend existiert** (§4.1). Womit
      Frontend-Komponenten geprüft werden, entscheidet sich mit dem Frontend — die
      Anforderung „Komponenten werden getestet" bleibt
- [ ] Unit: alle Checker inkl. `JUnitChecker` (über `IProcessRunner` aus Phase 2)
- [ ] Unit: `TaskCategoryService`, `TaskItemService`, `TaskTestService`,
      `SubmissionService`, `EvaluationService` mit gemockten Repositories
- [ ] Unit: Punkteberechnung mit Randfällen (0 Tests, alle bestanden, Restverteilung)
- [ ] Unit: Mapping-Logik (Entity ↔ DTO)
- [ ] Integration: Controller über `WebApplicationFactory`
- [ ] Integration: Repositories gegen echte PostgreSQL (Testcontainers)
- [ ] Component: Aufgabenliste, Aufgabenseite, Ergebnisseite, Admin-Formulare — Werkzeug
      offen, siehe oben
- [ ] GitHub Actions: Build + Test bei jedem Push (bei einem Frontend ausserhalb von .NET
      zusätzlich dessen Toolchain)
- [ ] Coverage-Report (coverlet ist bereits eingebunden)

### Phase 7 — Docker Compose, README, Abschluss

- [ ] `Dockerfile` Backend — inkl. JDK und JUnit-Standalone-JAR
- [ ] `Dockerfile` Frontend — Inhalt hängt am Frontend-Neustart (§4.1); bei einem
      Frontend ausserhalb von .NET kommt eine Node-Toolchain in den Build-Schritt
- [ ] `docker-compose.yml`: PostgreSQL + Backend + Frontend, Healthchecks, `depends_on`,
      benanntes Volume für die DB
- [ ] Konfiguration vollständig über Umgebungsvariablen (ConnectionString, ApiBaseUrl,
      Admin-Passwort) — nichts Fest-Verdrahtetes mehr. **`Cors:AllowedOrigins` steht noch
      auf den Ports des stillgelegten Frontends** (5072/7281)
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

---

## 10. Offene Entscheidungen

1. ~~**Clean-Code-Zuschnitt.**~~ **Entschieden in Phase 3:** `CharacterSet` und
   `NamingConventions` sind Teilprüfungen unter Clean Code, keine eigenen Kategorien
   mehr. Die Enum-Werte bleiben als Altlast stehen (siehe §5.6).
2. ~~**JUnit-Dateien für Teilnehmer sichtbar?**~~ **Entschieden in Phase 3:** pro Datei
   über `IsVisibleToParticipant` schaltbar, Standard `false`. Die öffentliche API liefert
   nur freigeschaltete Dateien aus; die Darstellung im Frontend fehlt noch — Phase 4.
3. ~~**Aufgaben-Vertrag.**~~ **Entschieden in Phase 3.1:** strukturiert als
   `ExpectedClassName` und `ExpectedMethods` auf `TaskItem`, geprüft vom
   `ContractChecker` als Teilprüfung der Kompilierbarkeit. Hervorgehobene Darstellung
   (Phase 4) und das Eingabefeld im Admin-Panel (Phase 5) fehlen noch.
4. **Sandbox-Tiefe.** Docker Compose containerisiert die *Anwendung*, isoliert aber
   nicht die einzelne Abgabe — `javac`/`java` laufen im Backend-Container.
   Für einen workshop-internen Betrieb vertretbar. Echte Isolation pro Abgabe
   (eigener Container je Auswertung) wäre eine spätere Ausbaustufe.
   → Vorschlag: v1 mit Timeouts und Prozesslimits, Hinweis in der README.
5. **Datenbank in Tests.** Testcontainers (realistisch, braucht Docker) oder
   EF InMemory (schnell, weicht aber von PostgreSQL ab)?
6. ~~**Wie viele Themes?**~~ **Entschieden in Phase 4.1:** Light und Dark, kein OLED.
   Die Entscheidung gilt weiter — OLED unterschied sich nur in drei Grauwerten vom
   Dark-Theme und verdreifachte den Aufwand bei jeder Kontrastprüfung.
7. ~~**Theme-Persistenz.**~~ **Entschieden in Phase 4.1, teilweise überholt:** Cookie
   statt `localStorage`, weil Blazor Server vorab rendert und der Server zu diesem
   Zeitpunkt nicht an `localStorage` herankommt. **Ob das Problem im neuen Frontend
   überhaupt besteht, hängt an der Renderart** — bei einem reinen Browser-Frontend
   reicht `localStorage` plus ein Inline-Skript im `<head>`.

### Neu offen durch den Frontend-Neustart — Stoff für den neuen Chat

8. **Erscheinungsbild.** Zuerst festlegen, wie es aussehen soll, dann das Werkzeug
   wählen. Umgekehrt ist es in Phase 4.1 schiefgegangen.
9. **Framework oder nur Komponentenbibliothek?** Ein Bibliothekswechsel unter Blazor
   lässt Backend, `Shared` als Vertrag und die Auth-Planung unberührt; ein Wechsel zu
   React/… zieht API-Vertrag, Enum-Serialisierung, CORS, Auth und Phase 7 nach sich
   (Tabelle in §4.1). Der Aufwandsunterschied ist der Kern der Entscheidung.
10. **API-Vertrag.** Bleibt `Shared` der Vertrag, oder wird er aus OpenAPI erzeugt?
    Davon hängt ab, ob `JsonStringEnumConverter` und `ProblemDetails` nötig werden.
11. **Auth für `api/admin/*`.** Der alte Plan setzte serverseitiges Rendern voraus
    (§8, Phase 5). Bei einem Browser-Frontend braucht es einen anderen Weg.
12. **Werkzeuge für die Umsetzung.** Welche MCP-Server oder Skills helfen wirklich —
    Komponenten-Vorschau, Screenshot-gestützte Prüfung, Zugriff auf die Doku der
    gewählten Bibliothek? In dieser Sitzung war der Screenshot-Kanal nicht verfügbar,
    geprüft werden konnte nur über `getComputedStyle`. Für Designarbeit ist das zu wenig.
13. **Phasenschnitt.** Teilnehmer-Sicht (Phase 4) und Admin-Panel (Phase 5) teilen sich
    dasselbe Frontend. Ob die Trennung so bleibt, ist offen.
