# Abnahme Phase 5 — Admin-Panel und Bestands-Transfer

Branch `phase-5-admin-panel`, Stand 2026-08-17.

Zweigeteilt wie bei Phase 4: **Teil A** listet, was automatisiert geprüft ist und was du
deshalb *nicht* nachklicken musst. **Teil B** ist das, wofür es menschliche Augen
braucht — vor allem Bewegung und Erscheinungsbild, denn Animationen kann ich in dieser
Umgebung grundsätzlich nicht prüfen (§6.1: eine nicht gezeichnete Browser-Ansicht friert
jede Animationsuhr ein, nachgemessen über `document.visibilityState`).

---

## Vorbereitung

**Neu in dieser Phase:** ohne `Admin__Password` in der `.env` startet das Backend nicht
mehr. Der Wert steht schon drin; ändere ihn vor dem Workshop.

```bash
.\scripts\stop-dev.ps1
```

```bash
.\scripts\start-dev.ps1
```

Testdaten — alle drei Skripte melden sich selbst an und fassen die Daten der jeweils
anderen nicht an:

```bash
.\tests\manual\seed-phase3.ps1
```

```bash
.\tests\manual\seed-pyramide.ps1
```

```bash
.\tests\manual\seed-oop.ps1
```

---

## Teil A — automatisiert geprüft, kein Handlungsbedarf

| Was | Ergebnis |
|---|---|
| `dotnet build SoopWorkshop.slnx` | 0 Warnungen, 0 Fehler |
| `dotnet test SoopWorkshop.slnx` | 270 Tests grün (221 → 270 in dieser Phase) |
| `npm run build` (enthält `tsc -b`) | durchläuft, Typprüfung gegen den erzeugten Vertrag sauber |
| `npm run lint` (oxlint) | keine Befunde |

Im Browser gemessen und hier **nicht** nachzuklicken:

| Was | Beleg |
|---|---|
| Cookie ist `HttpOnly` | `document.cookie` bleibt leer |
| `api/admin/*` ohne Cookie | 401, mit Cookie 200 |
| Abgelaufenes Cookie ≠ toter Server | Cookie gelöscht → Anmeldung; Backend gestoppt → „Der Server antwortet nicht" |
| Start ohne `Admin__Password` | bricht mit Meldung ab (ausgelöst und beobachtet) |
| Verborgene Aufgabe | bleibt in der Verwaltung sichtbar, fällt aus `GET /api/categories` heraus |
| Löschdialog | modal, Fokus auf „Abbrechen", Hintergrund per `elementFromPoint` unerreichbar, Escape schließt |
| Editor-Tastatur | Tab rückt ein, Escape + Tab verlässt das Feld (`defaultPrevented: false`) |
| Zeilennummern | 79 Zeilen Inhalt → 79 Nummern |
| Symbol-Suche | „schleife" → Repeat/Infinity, „konto" → Landmark/PiggyBank/Wallet/CreditCard |
| Gewichte-Vorschau | rechnet wie der `EvaluationScorer`, inkl. 43/57 ohne Funktionalität |
| Vorschau ehrlich | Admin-DTO und öffentlicher DTO sind für dieselbe Aufgabe gleich |
| Transfer verlustfrei | Export → DB leeren → Import → Fingerabdruck identisch |
| Transfer idempotent | derselbe Export erneut zusammengeführt → 0 angelegt, nichts verdoppelt |
| Bewertung nach Import | dieselben Punktzahlen (100 / 87 / 33) |

---

## Teil B — bitte selbst ansehen

### 1. Anmeldung

1. `http://localhost:5173/admin` aufrufen → Anmeldemaske.
2. Falsches Passwort → **„Das Passwort stimmt nicht."** *ohne* Anführungszeichen.
   (Standen dort welche, wäre der Accept-Kopf falsch — §9.)
3. Richtiges Passwort (aus der `.env`) → Übersicht.
4. Seite neu laden → bleibt angemeldet. **Abmelden** → wieder die Maske.

### 2. Kategorien

`Verwaltung → Kategorien`

1. Neue Kategorie anlegen → erscheint **verborgen** am Ende.
2. Auf das **Symbol** links klicken → Auswahl mit 135 Symbolen in acht Gruppen.
   Nach `schleife` suchen, ein Symbol wählen → es steht sofort in beiden Seitenleisten.
3. Umbenennen, mit den Pfeilen verschieben, sichtbar schalten.
4. **Löschen einer Kategorie mit Aufgaben** → der Dialog muss die Zahl der Aufgaben
   nennen *und* die Abgaben erwähnen. Hier **Abbrechen**.
5. Die eigene Testkategorie löschen.

### 3. Aufgabe anlegen und bearbeiten

1. Übersicht → **Neue Aufgabe** (oder „Aufgabe" in einer Kategoriezeile).
2. Titel und Beschreibung, anlegen → landet direkt im Editor, Aufgabe ist verborgen.
3. **Beschreibung**: Markdown eintippen, Vorschau darunter prüfen.
4. **Vertrag**: Klasse hinzufügen, benennen, eine Signatur ergänzen.
5. **Testfälle**: einen anlegen.
6. **JUnit-Dateien**: einmal *Aus Vorlage*, einmal *.java hochladen*.
   Im Code-Feld **Tab** drücken (rückt ein), dann **Escape** und **Tab** (springt heraus).
7. **Gewichte** verstellen → die Punktevorschau ändert sich mit.
8. **Speichern**, Seite neu laden → alles noch da.

### 4. Sichtbarkeit

1. Auswertung auf **Unit-Tests** stellen, alle JUnit-Dateien entfernen, speichern.
2. **Freischalten** drücken → die Ablehnung des Servers erscheint im Wortlaut.
3. Datei wieder hinterlegen, speichern, freischalten → klappt.

### 5. Vorschau und Probelauf

1. Im Editor oben rechts **Teilnehmer-Vorschau**. Die amberne Leiste muss deutlich sein.
2. Vergleiche mit der echten Teilnehmersicht derselben Aufgabe — gleicher Inhalt.
   Eine **nicht** freigeschaltete JUnit-Datei darf dort **nicht** auftauchen.
3. Zurück im Editor: **Probelauf**, eine Musterlösung aus
   `tests/manual/junit/loesungen/` hochladen.
   → **Hier bitte auf die Bewegung achten:** die Punktzahl muss von 0 hochzählen und
   der Kreis kurz federn. Das ist der Teil, den ich nicht prüfen kann.
4. Danach eine fehlerhafte Lösung, z. B. `bank-ohne-deckungspruefung` → genau eine
   Teilprüfung rot, mit „Erwartet false / Erhalten true".

### 6. Mehrklassen-Aufgabe

`OOP → Bankkonto`, als Teilnehmer:

1. `bank/Kunde.java` **und** `bank/Konto.java` **zusammen** hochladen → 100/100.
2. `bank-methode-in-falscher-klasse` → 33/100, und die Teilprüfung
   **„Die Methode 'getStand' steht in 'Konto'"** fällt durch.

### 7. Transfer

`Verwaltung → Transfer`

1. **Als Datei speichern** → die Datei landet im Download-Ordner. Kurz hineinsehen:
   lesbar, eingerückt, alle Kategorien drin.
2. Modus **Zusammenführen**, dieselbe Datei wählen → Vorschau zeigt „0 neu,
   alles aktualisiert". Einspielen → Bestand unverändert.
3. Eine Aufgabe im Panel umbenennen, dieselbe Datei erneut zusammenführen → der alte
   Name ist wieder da (die Datei gewinnt).
4. Modus **Ersetzen** wählen, Datei erneut → die Vorschau muss die Zahl der Abgaben
   nennen, und der Bestätigungsdialog ebenfalls. Hier **Abbrechen** genügt.
5. Eine kaputte Datei wählen (irgendeine `.txt` in `.json` umbenennen) → verständliche
   Meldung, kein Absturz.

### 8. Erscheinungsbild

Das ist dein Teil (§6.1) — Farben und Abstände sind bewusst nicht von mir gesetzt:

- Fenster auf **768 px** ziehen: die Seitenleiste klappt als Überlagerung ein, der
  Burger-Knopf erscheint, kein Querscroll.
- Mit **Tab** durch die Formulare: der Fokus ist überall sichtbar und geht nicht
  in eingeklappte Bereiche hinein.
- Die Einblendungen laufen und **verschlucken nichts**.

### 9. Zum Schluss

```bash
git status
```

Erwartet: sauber. `.env` und `node_modules` tauchen nicht auf.

Solution zusätzlich in Visual Studio oder Rider öffnen und bauen — die Kommandozeile
deckt die IDE-eigene Auflösung von `Directory.Build.props` nicht ab.

---

## Was in dieser Phase bewusst offen geblieben ist

- **Submissions-Übersicht** — auf Phase 6 geschoben. Sie braucht neue Endpunkte und
  hilft beim Pflegen der Aufgaben nicht.
- **Drag & Drop für die Reihenfolge** — durch Hoch/Runter-Knöpfe ersetzt. Drag & Drop
  löst dasselbe schlechter, weil es mit der Tastatur nicht bedienbar ist.
- **Monaco als Code-Editor** — der Editor hat Zeilennummern und eine brauchbare
  Tab-Taste; Syntaxhervorhebung bleibt eine spätere Ausbaustufe.
- **Das Anmelde-Cookie ist `Secure`.** Auf `localhost` nehmen Browser es auch über
  http an (gemessen); im Betrieb braucht es HTTPS — Phase 7.
