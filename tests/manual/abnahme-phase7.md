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
| Seite über `http://` | 200 `text/html` |
| Unterpfad direkt aufgerufen | 200 mit `index.html` (kein 404) |
| `/api/categories` durch den Proxy | 200 |
| `/health` durch den Proxy | `Healthy` |
| Port 443 | nicht erreichbar — es gibt nur http |
| Anmeldung durch den Proxy | 204, Cookie `samesite=lax; httponly`, **kein** `secure` |
| Geschützter Endpunkt mit Cookie | 200 · ohne Cookie 401 |
| Abgaben-Übersicht mit Cookie | 200 |
| Abgabe im Modus `Both` | durch `javac` und JUnit im Container ausgewertet |
| Umlaute | `Die Größe der Summe beträgt: 7` — unversehrt |
| Datei über 1 MB | deutscher Satz als `text/plain`, nicht nginx' HTML |

Das **fehlende** `secure` ist hier der wichtige Teil: ein Secure-Cookie würde
über http gar nicht zurückgeschickt, und die Anmeldung funktionierte von keinem
Rechner im Netz. `CookieSecurePolicy.SameAsRequest` sorgt dafür von selbst —
über https würde dasselbe Cookie `Secure` tragen.

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

Es sind fünf Pflichtschritte. Besonders hinsehen bei:

- **Schritt 1 (Docker prüfen)** — reicht der Hinweis, wenn das Compose-Plugin
  fehlt? Auf einer VM mit vorinstalliertem Docker ist das der wahrscheinlichste
  Stolperstein
- **Schritt 3 (`.env`)** — ist klar, welches der beiden Passwörter das ist, mit
  dem man sich später anmeldet?
- **Schritt 4 (Starten)** — zeigt `ps` am Ende wirklich zweimal `healthy`? Wie
  lange hat der erste Build gebraucht?
- **Der Teilnehmerblick** — `http://<ip-der-vm>` von einem fremden Rechner
  öffnen. Die Seite muss **direkt** laden, ohne dass irgendwo etwas zu
  bestätigen ist. Das „Nicht sicher" in der Adressleiste ist erwartet und kein
  Befund (Abschnitt „Warum kein HTTPS")

Alles unter *Optional* ist ausdrücklich **nicht** Teil der Abnahme. Wenn dabei
etwas nicht klappt, ist das eine Notiz für später, kein Blocker.

### 2.5 Vom zweiten Rechner verwalten

Der Punkt, an dem der ganze Aufbau hängt und der sich lokal nicht prüfen lässt.

Von einem **anderen** Rechner im Netz (nicht vom Server):

1. `http://<ip-der-vm>/admin` öffnen
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
- **Chrome und Edge schreiben „Nicht sicher" in die Adressleiste.** Das ist bei
  http immer so und lässt sich nicht wegkonfigurieren — ein Schloss gibt es nur
  mit einem vertrauenswürdigen Zertifikat. Es ist ein Label, keine Warnseite:
  die Seite lädt sofort, es ist nichts zu klicken. Die Abwägung steht in
  `docs/server-aufsetzen.md` unter „Warum kein HTTPS".
- **Das Admin-Passwort läuft im Klartext durchs LAN.** Folge derselben
  Entscheidung, offen dokumentiert. Deshalb gehört der Aufbau nicht ins Internet.
- **Eine Abgabe erreicht die Datenbank.** Das interne Netz sperrt LAN und
  Internet aus, nicht die Datenbank. Echte Isolation je Abgabe ist eine spätere
  Ausbaustufe.
