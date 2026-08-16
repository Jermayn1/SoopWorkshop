# Abnahme Phase 4 — Teilnehmer-Frontend „Soop Judge"

Branch `phase-4-frontend-neustart`, Stand 2026-08-16.

Diese Anleitung ist zweigeteilt: **Teil A** listet, was automatisiert geprüft ist und
was du deshalb *nicht* nachklicken musst. **Teil B** ist das, wofür es menschliche
Augen braucht — vor allem Bewegung, denn die kann ich in dieser Umgebung
grundsätzlich nicht prüfen (§6.1).

---

## Vorbereitung

```bash
.\scripts\stop-dev.ps1
```

```bash
.\scripts\start-dev.ps1
```

Erwartet: Build ohne Warnungen, zwei Fenster (API und Frontend), am Ende die Übersicht
mit `Frontend http://localhost:5173`. Beim ersten Start läuft einmalig `npm install`.

Danach die Testdaten anlegen — beide Skripte sind mehrfach ausführbar und fassen die
Daten des jeweils anderen nicht an:

```bash
.\tests\manual\seed-phase3.ps1
```

```bash
.\tests\manual\seed-pyramide.ps1
```

---

## Teil A — automatisiert geprüft, kein Handlungsbedarf

| Was | Ergebnis |
|---|---|
| `dotnet build SoopWorkshop.slnx` | 0 Warnungen, 0 Fehler |
| `dotnet test SoopWorkshop.slnx` | 221 Tests grün |
| `npm run build` (enthält `tsc -b`) | durchläuft, Typprüfung sauber |
| `npm run lint` (oxlint) | keine Befunde |
| OpenAPI-Vertrag | 27 von 27 Operationen mit Erfolgs-Schema, 29 Schemata |
| Enums über die Leitung | `"Easy"`, `"ConsoleOnly"` — Vertrag und Laufzeit gegengeprüft |
| Kontraste, hell | 13 Farbpaare gemessen, alle über der Schwelle |
| Punktekreis-Verläufe | 3,65 / 3,76 / 3,75 : 1 gegen die weiße Zahl (nötig 3:1) |
| Responsive 1280 und 768 | kein Querscroll, nichts sprengt die Karte |
| Burger-Menü | Escape schließt, Fokus springt, Inhalt wird `inert` |
| Fachlicher Durchlauf | Musterlösungen 100/100, Fehlerfälle mit lesbarer Begründung |
| Upload-Ablehnung | Servermeldung erreicht das Frontend im Wortlaut |
| PowerShell-Skripte | `start-dev`, `stop-dev`, `seed-pyramide` fehlerfrei geparst |

---

## Teil B — bitte selbst ansehen

### 1. Seitenleiste und Kategorien

- Zwei Kategorien: **Phase 3 - Beispiele** und **Schleifen**.
- Auf eine Kategoriezeile klicken → die Aufgaben **fahren weich zusammen**, der Pfeil
  dreht sich um 90°. Noch einmal klicken → sie fahren wieder auf.
- ⚠️ **Genau hier bitte hinsehen:** Läuft das als Bewegung ab oder springt es? Ich
  konnte das nicht prüfen — eine nicht gezeichnete Browser-Ansicht friert jede
  Animationsuhr ein, und das sieht identisch aus wie eine kaputte Animation.
- Eine Aufgabe anklicken, während ihre Kategorie eingeklappt ist (über einen direkten
  Link) → die Kategorie klappt von selbst auf.

### 2. Aufgabenseite — „Sternchen-Pyramide"

- Kopfzeile: Chips **MITTEL** und **UNIT-TESTS** blenden von oben ein, die Überschrift
  kommt von links, die Beschreibung blendet auf.
- Die Beschreibung ist **Markdown**: `Main` und `zeichnePyramide` stehen in Code-Auszeichnung,
  der Beispielblock mit der Pyramide ist ein Codeblock, „gibt zurück" ist fett.
- **Was geprüft wird** zeigt Klasse `Main` und beide Signaturen.
- **Diese Tests laufen gegen deine Abgabe** lässt sich aufklappen und zeigt
  `PyramideTest.java` im Quelltext.
- **Tipps & Hilfestellungen** aufklappen → vier Tipps erscheinen nacheinander versetzt.
- Über die Abwurfzone fahren → das Symbol wächst und kippt leicht, die Fläche hellt auf.

### 3. Abgabe und Grenzen

- `tests\manual\junit\loesungen\pyramide\Main.java` hineinziehen → die Zone wechselt auf
  „1 Datei bereit", darunter erscheint die Datei mit Größe und einem X zum Entfernen.
- Eine beliebige `.txt` hineinziehen → gelber Kasten: **„'…' ist keine .java-Datei."**
  Die Datei verschwindet *nicht* kommentarlos.
- Dieselbe Datei zweimal → **„… ist bereits ausgewählt."**
- Unter der Zone stehen die Grenzen: `.java · höchstens 10 Dateien · 1,0 MB je Datei ·
  10,0 MB gesamt`.

### 4. Der Durchlauf

- **Jetzt prüfen** → kurz „In der Warteschlange", dann **„Wird gerade geprüft"**.
  Die beiden Texte müssen sich unterscheiden.
- Ergebnis: **100 von 100**.
  - Der Kreis fährt mit einer Feder hoch und dreht sich dabei gerade.
  - Die Zahl **zählt von 0 hoch**.
  - Oben rechts erscheint der Pokal und **wackelt dauerhaft** (ab 80 Punkten).
  - Die drei Kategoriekarten steigen versetzt ein, ihre Balken wachsen von 0.
- Alle Kategorien sind zugeklappt, weil alles bestanden ist.

### 5. Ein Fehlerfall mit Substanz

- Zurück zur Aufgabe, `pyramide-linksbuendig\Main.java` abgeben.
- Erwartet: **68 von 100**, Funktionalität rot, die Karte ist **von selbst aufgeklappt**.
- Zwei Teilprüfungen mit rotem Kreuz, beide zur Einrückung. Darunter je **Erwartet** und
  **Erhalten** untereinander — nie nur eines von beidem.
- Bestandene Teilprüfungen zeigen **nichts** außer dem Haken.

### 6. Die drei Ausgänge (der wichtigste Test)

- Das API-Fenster schließen, dann im Browser neu laden.
  → **„Der Server antwortet nicht"** mit Knopf *Erneut versuchen*.
  → **Nicht** „Diese Aufgabe gibt es nicht". Das war der teuerste Fehler aus 4.1.
- API wieder starten, dann eine erfundene Adresse aufrufen:
  `http://localhost:5173/aufgaben/00000000-0000-0000-0000-000000000000`
  → **„Diese Aufgabe gibt es nicht"**.

### 7. Schmales Fenster

- Fenster auf etwa halbe Breite ziehen (unter 1024 px).
  → Seitenleiste verschwindet, oben erscheint ein Burger-Knopf.
- Burger anklicken → Leiste als Überlagerung, dahinter abgedunkelt.
- **Escape** drücken → schließt. Klick auf den dunklen Grund → schließt ebenfalls.
- Eine Aufgabe darin anklicken → die Überlagerung schließt sich von selbst.

### 8. Tastatur

- Auf der Aufgabenseite mit **Tab** durchgehen: jedes Element bekommt einen sichtbaren
  Rahmen, die Reihenfolge folgt der Leserichtung.
- Bei eingeklappter Kategorie: die Aufgaben darin sind **nicht** antabbar.

### 9. Zum Schluss

- Solution zusätzlich in **Visual Studio oder Rider** öffnen und bauen — die
  Kommandozeile deckt die IDE-eigene Auflösung von `Directory.Build.props` nicht ab.
- `git status` → sauber, ohne `.env` und ohne `node_modules`.

---

## Bewusst nicht umgesetzt

Kein Befund, sondern Absicht — steht so in CLAUDE.md §8:

- **Kein Dunkelmodus.** Das Referenzprojekt kannte nur Hell, und das Erscheinungsbild
  wird von Hand nachgezogen. Ein zweiter Satz Farben davor wäre Arbeit auf Verdacht.
- **Keine Handy-Optimierung.** Der Workshop läuft an Laptops. Das Burger-Menü deckt
  schmale Fenster ab, echte Telefonbreiten sind nicht geprüft.
- **Keine eigene Aufgabenübersicht.** Die Liste lebt in der Seitenleiste; die Startseite
  ist eine Begrüßung. Bei drei Kategorien reicht das.
- **Keine Frontend-Tests.** Vitest + Testing Library sind für Phase 6 vorgesehen.
