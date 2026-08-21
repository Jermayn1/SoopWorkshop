# SoopWorkshop

Automatisches Auswertungstool für Java-Aufgaben im SOOP-Workshop (Strukturierte
Objektorientierte Programmierung).

Teilnehmer laden ihre `.java`-Dateien hoch, das Tool kompiliert sie, prüft sie
gegen hinterlegte Testfälle und JUnit-Tests und gibt ein kategorisiertes,
nachvollziehbares Feedback zurück. Der Betreuer pflegt Aufgaben, Testfälle und
Bewertungsgewichte über ein Web-Panel — ohne Datenbankzugriff.

Betrieb **workshop-intern**, nicht öffentlich erreichbar.

---

## Was es kann

**Für Teilnehmer**

- Aufgaben nach Kategorien, mit Beschreibung, Schwierigkeitsgrad und Tipps
- Der geforderte Vertrag ist sichtbar: welche Klassen und Methoden erwartet
  werden — bewertet wird nur, was auch dasteht
- Mehrere `.java`-Dateien per Auswahl oder Drag & Drop; abgelehnte Dateien
  verschwinden nie kommentarlos, sondern nennen den Grund
- Ergebnis nach Kategorien, mit *Eingabe*, *Erwartet* und *Erhalten* je
  Teilprüfung — Compilerausgaben und Stacktraces inklusive

**Für den Betreuer**

- Kategorien und Aufgaben anlegen, ändern, sortieren, sichtbar schalten
- Konsolen-Testfälle und JUnit-Dateien im Editor pflegen, aus Vorlagen oder per
  Datei-Upload
- Bewertungsgewichte je Aufgabe, mit Live-Vorschau der Normierung auf 100
- **Vorschau** (die Aufgabe exakt so, wie Teilnehmer sie sehen) und
  **Probelauf** (eigene Musterlösung durch die echte Auswertung schicken)
- **Abgaben-Übersicht** mit Status und Punktzahl
- Gesamten Aufgabenbestand als eine JSON-Datei aus- und einspielen

**Bewertung**

Drei Kategorien — Clean Code, Kompilierbarkeit, Funktionalität —, gewichtet und
auf 100 Punkte normiert. Keine Gratispunkte: eine Aufgabe ohne Prüfungen einer
Kategorie verteilt deren Gewicht auf die übrigen, statt sie geschenkt zu
vergeben. Volle Punkte gibt es nur, wenn alle Teilprüfungen bestanden sind.

---

## Aufsetzen

**Voraussetzung ist nur Docker** (Engine mit Compose v2). .NET, Node und das
JDK stecken in den Images — auf dem Server ist nichts weiter zu installieren.
Auf den Teilnehmerrechnern ebenfalls nicht: ein Browser genügt.

**1 — Zugangsdaten setzen.** Vorlage kopieren:

```bash
cp .env.example .env
```

Darin zwei Werte setzen, beide sind Pflicht:

| Schlüssel | Wofür |
|---|---|
| `POSTGRES_PASSWORD` | Passwort der Datenbank |
| `Admin__Password` | schützt `/admin` und alle `api/admin/*` |

Fehlt `Admin__Password`, **startet das Backend nicht** — lieber ein klarer
Abbruch als ein stiller Start ohne Zugangsschutz. Die übrigen Werte in der
`.env` (Zeitgrenzen, Parallelität, `HTTP_PORT`) haben brauchbare Standardwerte.

**2 — Starten.**

```bash
docker compose -f docker-compose.yml up -d --build
```

Das `-f` ist kein Schmuck: ohne die Angabe zieht Compose zusätzlich
`docker-compose.override.yml` mit und veröffentlicht damit den Datenbank-Port.
Die Override-Datei ist ausschließlich für die lokale Entwicklung gedacht.

Der erste Lauf baut beide Images und dauert einige Minuten. Danach:

```bash
docker compose ps
```

Alle drei Dienste müssen `healthy` melden. Das Tool ist dann unter
`http://<ip-des-servers>` erreichbar, die Verwaltung unter
`http://<ip-des-servers>/admin`.

**Nützlich im Betrieb:**

```bash
docker compose logs -f backend
```

```bash
docker compose down
```

`down` stoppt alles, die Daten bleiben im Volume. Nur `down -v` löscht sie.

### Ports

**Im Betrieb ist genau ein Port nach außen offen.** Ein nginx liefert die Seite
aus **und** reicht `/api` an das Backend weiter — Frontend und API haben damit
denselben Ursprung. Backend und Datenbank liegen in einem Docker-Netz ohne
Route nach draußen und sind von außerhalb des Servers nicht erreichbar.

| | Port | Erreichbar |
|---|---|---|
| Frontend + API (nginx) | `80`, über `HTTP_PORT` in der `.env` änderbar | aus dem Netz |
| Backend | `8080` | nur containerintern |
| PostgreSQL | `5432` | nur containerintern |

In der **Entwicklung** laufen Backend und Frontend außerhalb der Container:

| | Adresse |
|---|---|
| Frontend (Soop Judge) | `http://localhost:5173` |
| Verwaltung | `http://localhost:5173/admin` |
| Backend API | `http://localhost:5120`, `https://localhost:7212` |
| API-Doku (Scalar, nur Development) | `http://localhost:5120/scalar` |
| PostgreSQL | `127.0.0.1:5432` |

Der Frontend-Port steht an zwei Stellen — in `vite.config.ts` (`strictPort`)
und im Backend unter `Cors:AllowedOrigins`. Wird er nur an einer geändert,
blockt der Browser jede Anfrage, und der Fehler sieht nach einem kaputten
Backend aus.

---

## Lokal entwickeln

**Voraussetzungen:** .NET 10 SDK, Node.js, Docker, JDK 21 im `PATH`
(`javac` und `java` werden je Abgabe als Prozess aufgerufen).

`.env` wie oben anlegen, dann nur die Datenbank hochziehen:

```bash
docker compose up -d db
```

Backend und Frontend laufen daneben direkt auf dem Rechner — am besten in zwei
Fenstern, damit die Protokolle lesbar bleiben:

```bash
dotnet run --project src/SoopWorkshop.Backend.API
```

```bash
npm --prefix src/SoopWorkshop.Frontend run dev
```

Migrationen anwenden — **ohne** `--startup-project`, da der Kontext zur
Entwurfszeit über `AppDbContextFactory` gebaut wird:

```bash
dotnet ef database update --project src/SoopWorkshop.Backend.Infrastructure
```

Nach jeder Änderung an Controllern oder DTOs die TypeScript-Typen neu erzeugen,
bei **laufendem** Backend:

```bash
npm --prefix src/SoopWorkshop.Frontend run api:types
```

**Das Backend hält seine DLLs.** Ein `dotnet build` bei laufendem Backend
scheitert mit `CS2012 … used by another process` — erst stoppen, dann bauen.

---

## Architektur

Clean Architecture im Backend, daneben ein eigenständiges React-Frontend.

```
SoopWorkshop.Shared                  DTOs, Enums, Konstanten
SoopWorkshop.Backend.Domain          Entities — kennt nur Shared
SoopWorkshop.Backend.Application     Services, Interfaces, Result<T>
SoopWorkshop.Backend.Infrastructure  EF Core, Repositories, Java-Checker,
                                     Warteschlange, Bestands-Transfer
SoopWorkshop.Backend.API             Controller, Middleware
SoopWorkshop.Frontend                React 19 + Vite + TypeScript + Tailwind 4
tests/SoopWorkshop.Tests             xUnit
```

**Abhängigkeitsregeln:** Domain kennt kein EF Core. Application definiert
Interfaces, Infrastructure implementiert sie. Frontend und Backend sprechen
ausschließlich über HTTP; der Vertrag entsteht aus OpenAPI und wird als
TypeScript erzeugt.

**Ablauf einer Auswertung**

```
Upload → SubmissionService → Warteschlange (begrenzt)
       → EvaluationWorker (n parallel) → JavaAnalyzer
       → Checker (Vertrag, Kompilierbarkeit, Zeichensatz,
                  Namenskonventionen, Testfälle, JUnit)
       → EvaluationScorer → gespeichert → Frontend pollt den Status
```

Externe Prozesse (`javac`, `java`, JUnit) laufen ausschließlich über
`IProcessRunner`, nie direkt über `Process.Start`.

---

## Tests

```bash
dotnet test SoopWorkshop.slnx
```

```bash
npm --prefix src/SoopWorkshop.Frontend test
```

401 Projekt-Tests, davon 98 gegen ein echtes PostgreSQL aus dem Container
(Testcontainers), dazu 159 Frontend-Tests mit Vitest.

**Die Projekt-Tests brauchen Docker.** Ohne Docker geht der schnelle Lauf:

```bash
dotnet test SoopWorkshop.slnx --filter "Category!=Integration"
```

---

## Grenzen — bitte lesen, bevor du es aufsetzt

**Das System ist für ein geschlossenes lokales Netz gedacht und gehört nicht ins
Internet.**

- **Fremder Code wird ausgeführt.** Jede Abgabe wird kompiliert und gestartet.
  Der Backend-Container hat deshalb keine Route ins LAN und keine ins Internet,
  dazu Grenzen für Speicher, CPU und Prozessanzahl sowie Zeitgrenzen je Lauf.
  Was das **nicht** leistet: eine Abgabe erreicht weiterhin die Datenbank und
  kann den Container belasten. Echte Isolation je Abgabe wäre eine spätere
  Ausbaustufe. Behandle die VM als nicht vertrauenswürdig — Snapshot vorher.
- **Ein Passwort, keine Benutzerverwaltung.** Der Workshop hat genau einen
  Betreuer. Es gibt keine Rollen, keine Nutzerkonten, kein Zurücksetzen.
- **Der Betrieb läuft über http, nicht https.** Das ist eine bewusste
  Entscheidung: für einen internen Namen gibt es kein Zertifikat, dem Browser
  von sich aus trauen, und die Alternativen wären für die Teilnehmer schlechter
  (selbstsigniert = ganzseitige Warnung auf jedem Rechner; eigene CA =
  Wurzelzertifikat von Hand auf jeden Rechner). Der Preis: das Admin-Passwort
  läuft im Klartext durchs LAN. Nachrüsten ist später ein
  Konfigurationsschritt — das Backend wertet `X-Forwarded-Proto` bereits aus.
- **Abgaben überleben keinen Neustart mitten in der Auswertung.** Sie werden
  danach als fehlgeschlagen markiert, mit einem Hinweis für den Teilnehmer.

---

## Lizenz

[MIT](LICENSE)
