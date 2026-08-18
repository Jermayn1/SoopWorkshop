# SoopWorkshop

Automatisches Auswertungstool für Java-Aufgaben im SOOP-Workshop (Strukturierte
Objektorientierte Programmierung).

Teilnehmer laden ihre `.java`-Dateien hoch, das Tool kompiliert sie, prüft sie
gegen hinterlegte Testfälle und JUnit-Tests und gibt ein kategorisiertes,
nachvollziehbares Feedback zurück. Der Betreuer pflegt Aufgaben, Testfälle und
Bewertungsgewichte über ein Web-Panel — ohne Datenbankzugriff.

Entstanden als Lernprojekt neben der Ausbildung zum Fachinformatiker für
Anwendungsentwicklung. Betrieb **workshop-intern**, nicht öffentlich erreichbar.

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

## Auf einem Server aufsetzen

**→ [docs/server-aufsetzen.md](docs/server-aufsetzen.md)**

**Fünf Schritte, rund 15 Minuten** — davon zehn Wartezeit beim Bauen. Danach
erreichen die Teilnehmer das Tool über `http://<ip-der-vm>`: **kein Zertifikat,
kein DNS-Eintrag, und auf den Teilnehmerrechnern ist nichts einzurichten.**

Kurzfassung, wenn Docker schon läuft:

```bash
cp .env.example .env
```

Darin `POSTGRES_PASSWORD` und `Admin__Password` setzen, dann:

```bash
docker compose -f docker-compose.yml up -d --build
```

Voraussetzung ist nur Docker. .NET, Node und das JDK stecken in den Images.
DNS-Name, Firewall, Sicherung und HTTPS stehen in der Anleitung als optionale
Schritte — sie werden für den Betrieb nicht gebraucht.

---

## Lokal entwickeln

**Voraussetzungen:** .NET 10 SDK, Node.js, Docker, JDK 21 im `PATH`.

Einmalig `.env.example` nach `.env` kopieren und `POSTGRES_PASSWORD` sowie
`Admin__Password` setzen. Dann:

```bash
docker compose up -d db
```

```bash
.\scripts\start-dev.ps1
```

Startet Backend und Frontend in je einem eigenen Fenster.

| Dienst | Adresse |
|---|---|
| Frontend (Soop Judge) | `http://localhost:5173` |
| Verwaltung | `http://localhost:5173/admin` |
| Backend API | `http://localhost:5120` |
| API-Doku (Scalar, nur Development) | `http://localhost:5120/scalar` |

Alle Prüfungen in einem Durchgang — Build, Projekt-Tests, Frontend-Build,
Frontend-Tests, Linter:

```bash
.\scripts\pruefe-alles.ps1
```

Er bricht **nicht** beim ersten Fehler ab und nennt am Ende jeden Schritt mit
Ergebnis. `-OhneDocker` lässt die Integrationstests aus, `-MitCoverage` schreibt
Berichte nach `artifacts/coverage/`.

Nach jeder Änderung an Controllern oder DTOs, bei laufendem Backend:

```bash
npm --prefix src/SoopWorkshop.Frontend run api:types
```

Weitere Befehle, Konventionen und die Entwicklungshistorie stehen in
[CLAUDE.md](CLAUDE.md).

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

**Im Betrieb** liefert ein nginx die Seite aus **und** reicht `/api` an das
Backend weiter. Frontend und API haben damit denselben Ursprung: kein CORS, und
das Anmelde-Cookie überquert keine Origin-Grenze. Backend und Datenbank liegen
in einem Docker-Netz ohne Route nach draußen.

---

## Tests

```bash
dotnet test SoopWorkshop.slnx
```

400 Projekt-Tests, davon 98 gegen ein echtes PostgreSQL aus dem Container
(Testcontainers), dazu 145 Frontend-Tests mit Vitest.

**Die Projekt-Tests brauchen Docker.** Ohne Docker geht der schnelle Lauf:

```bash
dotnet test SoopWorkshop.slnx --filter "Category!=Integration"
```

Coverage wird gemessen, nicht erzwungen — es gibt bewusst keine Prozentschwelle.

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
