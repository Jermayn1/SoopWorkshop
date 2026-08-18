# Abnahme Phase 7 — Auslieferung und Betrieb

Stand: 2026-08-18, Branch `phase-7-auslieferung`.

Phase 7 macht das Projekt auslieferbar. Am fachlichen Verhalten ändert sich für
Teilnehmer **nichts** — für den Betreuer kommen die Abgaben-Übersicht und zwei
korrigierte Statuscodes dazu.

---

## 1. Was bereits automatisiert geprüft ist

```powershell
.\scripts\pruefe-alles.ps1
```

400 Projekt-Tests (98 gegen ein echtes PostgreSQL), 145 Frontend-Tests, Build
warnungsfrei, Typprüfung und Linter sauber. Dieselben Schritte laufen bei jedem
Push als GitHub Action, dazu ein Job, der beide Images baut und prüft, dass
`javac`, `java` und das JUnit-JAR im Backend-Image liegen.

**Am laufenden Container-Stapel nachgemessen** (nicht behauptet):

| Nachweis | Ergebnis |
|---|---|
| Migration auf leerer Datenbank | 8 Migrationen angewendet, Backend `healthy` |
| Backend → Internet (Name) | `curl` Exit 6 — nicht auflösbar |
| Backend → Internet (IP) | `curl` Exit 7 — keine Route |
| Backend → LAN-Gateway | `curl` Exit 7 — keine Route |
| Backend → Datenbank | `curl` Exit 52 — erreichbar |
| `http://…` | 301 auf `https` |
| Unterpfad direkt aufgerufen | 200 mit `index.html` (kein 404) |
| `/api/categories` durch den Proxy | 200 |
| `/health` durch den Proxy | `Healthy` |
| Anmeldung durch den Proxy | 204, Cookie `secure; samesite=lax; httponly` |
| Geschützter Endpunkt mit Cookie | 200 · ohne Cookie 401 |
| Abgabe im Modus `Both` | **100/100** durch `javac` und JUnit im Container |
| Umlaute | `Die Größe der Summe beträgt: 7` — unversehrt |
| Datei über 1 MB | deutscher Satz als `text/plain`, nicht nginx' HTML |

Das `secure` im Cookie ist dabei der Beleg, dass `X-Forwarded-Proto` von nginx
über `UseForwardedHeaders` bis zur Cookie-Politik durchkommt.

---

## 2. Was menschliche Augen brauchen

### 2.1 Abgaben-Übersicht *(neu)*

Voraussetzung: es gibt mindestens eine Abgabe. Sonst vorher eine erzeugen —
`tests\manual\seed-phase3.ps1`, dann eine Lösung aus
`tests\manual\junit\loesungen\` hochladen.

1. `/admin` öffnen, anmelden
2. In der Seitenleiste **Abgaben** anklicken

**Erwartet:** eine Tabelle, neueste zuerst, mit Zeitpunkt, Aufgabe und
Kategorie, Statusmarke und Punktzahl.

3. Filter **Status** auf *Fehlgeschlagen* stellen

**Erwartet:** nur fehlgeschlagene Abgaben; steht die Liste leer, erscheint
„Zu diesem Filter gibt es keine Abgaben." — **nicht** „Es wurde noch nichts
abgegeben." Die beiden Sätze sind verschieden, und das ist Absicht.

4. Filter zurück auf *Alle*, bei einer fertigen Abgabe auf **Ergebnis** klicken

**Erwartet:** die normale Ergebnisseite — dieselbe, die der Teilnehmer sieht.

5. Bei einer Abgabe ohne Auswertung (Status *In der Warteschlange*) auf die
   Punktespalte achten

**Erwartet:** ein Gedankenstrich, **keine 0**. Null Punkte wären eine Aussage
über die Lösung; der Strich sagt nur „noch nicht bewertet".

6. Gibt es mehr als 25 Abgaben: **Weiter** anklicken

**Erwartet:** die Zählung („26–50 von 73") stimmt, keine Zeile taucht doppelt
auf, **Zurück** ist auf Seite 1 ausgegraut.

### 2.2 Sichtbarkeit: 400 statt 404 *(geänderter Statuscode)*

1. Neue Aufgabe anlegen mit Modus **UnitTestOnly**, keine JUnit-Datei hinterlegen
2. Speichern, dann **Sichtbar schalten** versuchen

**Erwartet:** eine Meldung, die sagt, dass eine JUnit-Datei fehlt. Die Aufgabe
bleibt verborgen.

> Vorher kam an dieser Stelle intern ein 404 — für eine Aufgabe, die offen im
> Editor liegt. Sichtbar war das nicht, weil das Panel die Meldung unabhängig
> vom Ausgang anzeigt; im Frontend kam sie aber als „gibt es nicht" an.

### 2.3 Dateinamen: Groß- und Kleinschreibung *(geändertes Verhalten)*

1. Eine Aufgabe öffnen, `Main.java` auswählen
2. Eine zweite Datei namens `main.java` **per Drag & Drop** fallen lassen

**Erwartet:** „'main.java' ist bereits ausgewählt." Die Datei wird verworfen.

> Vorher ließ der Browser beide durch, und erst der Server lehnte ab — eine
> Ablehnung für etwas, das die Seite eben noch angenommen hatte. (Über den
> Dateidialog lässt sich das nicht auslösen, dort greift `accept`.)

### 2.4 Der Durchlauf auf dem Server

**Das ist die eigentliche Abnahme dieser Phase.** Sie prüft nicht Code, sondern
ob die Anleitung trägt.

**`docs/server-aufsetzen.md` einmal komplett abarbeiten**, auf einer frischen
VM oder einem zurückgesetzten Snapshot. Von oben nach unten, ohne zu
überspringen, und bei jeder Kontrolle wirklich hinsehen.

Eine Anleitung, die nur der Autor gelesen hat, ist keine Anleitung. Was unklar
ist, gehört notiert und nachgebessert — das ist kein Nebenprodukt der Abnahme,
sondern ihr Zweck.

Besonders hinsehen bei:

- **Abschnitt 5 (DNS)** — passt der beschriebene Weg zu eurem Netz? Der
  Normalfall ist der vorhandene interne DNS; ist das bei euch ein anderer, muss
  es dort stehen
- **Abschnitt 6.4 (Wurzelzertifikat)** — auf einem Teilnehmerrechner
  durchspielen. Danach muss `https://soop.workshop` **ohne Warnung** öffnen, mit
  Schloss. Firefox braucht einen eigenen Schritt
- **Abschnitt 9 (Absichern)** — `nmap` von einem anderen Rechner: Port 5432 muss
  zu sein

### 2.5 Vom zweiten Rechner verwalten

Der Punkt, an dem der ganze Aufbau hängt und der sich lokal nicht prüfen lässt.

Von einem **anderen** Rechner im Netz (nicht vom Server):

1. `https://soop.workshop/admin` öffnen
2. Mit `Admin__Password` anmelden
3. Eine Kategorie anlegen und wieder löschen

**Erwartet:** die Anmeldung funktioniert und hält. Kommt nach dem Anmelden
sofort wieder die Passwortabfrage, wird das Cookie nicht zurückgeschickt —
dann stimmt etwas an `X-Forwarded-Proto` oder an der Cookie-Politik nicht.

### 2.6 Neustart der VM

1. `sudo reboot`
2. Warten, dann `docker compose -f docker-compose.yml ps`

**Erwartet:** alle drei Dienste wieder oben, `db` und `backend` `healthy`, die
Aufgaben sind noch da.

3. Eine Abgabe, die zum Zeitpunkt des Neustarts lief, ansehen

**Erwartet:** Status *Fehlgeschlagen* mit dem Hinweis, dass ein Neustart die
Auswertung abgebrochen hat und erneut eingereicht werden soll. **Das ist kein
Fehler, sondern das dokumentierte Verhalten.**

---

## 3. Bekannte Grenzen — nicht als Fehler melden

- **Oberhalb von 10 MB antwortet nginx mit einer englischen 413-Seite.** Darunter
  kommt der deutsche Satz des Servers durch. Über die Oberfläche ist der Fall
  nicht erreichbar, das Frontend blockt vorher.
- **Ohne Wurzelzertifikat zeigt der Browser eine Warnung.** Beabsichtigt: HSTS
  ist bewusst nicht gesetzt, damit die Seite auf solchen Rechnern überhaupt
  erreichbar bleibt.
- **Eine Abgabe erreicht die Datenbank.** Das interne Netz sperrt LAN und
  Internet aus, nicht die Datenbank. Echte Isolation je Abgabe ist eine spätere
  Ausbaustufe.
- **Der erste Start ohne Zertifikat erzeugt ein selbstsigniertes** und sagt das
  laut im Log. Gewollt, damit `docker compose up` auf einem frischen Rechner
  etwas Benutzbares ergibt.
