# SoopWorkshop auf einer VM aufsetzen

Fünf Schritte. Rund 15 Minuten, davon zehn Wartezeit beim Bauen.

Danach erreichen die Teilnehmer das Tool über `http://<ip-der-vm>` — kein
Zertifikat, kein DNS-Eintrag, und auf den Teilnehmerrechnern ist nichts
einzurichten.

Für Absicherung, Sicherung, einen DNS-Namen und HTTPS gibt es
[docs/betrieb.md](betrieb.md). Nichts davon wird gebraucht, damit es läuft.

---

## 1. Docker prüfen

```bash
docker compose version
```

Kommt eine Version, weiter zu Schritt 2. Kommt ein Fehler, fehlt das
Compose-Plugin — dann [Docker nachinstallieren](betrieb.md#docker-nachinstallieren).

Sagt jeder Docker-Befehl „permission denied":

```bash
sudo usermod -aG docker $USER
```

> Danach **einmal ab- und wieder anmelden.** Gruppen gelten erst in einer neuen
> Sitzung.

---

## 2. Projekt holen

```bash
sudo apt-get update && sudo apt-get install -y git
```

```bash
git clone https://github.com/Jermayn1/SoopWorkshop.git && cd SoopWorkshop
```

---

## 3. Zwei Passwörter setzen

```bash
cp .env.example .env
```

Passwörter würfeln lassen, nicht ausdenken — der Befehl gibt eines aus, ruf ihn
zweimal auf:

```bash
openssl rand -base64 24
```

```bash
nano .env
```

| Schlüssel | Wofür |
|---|---|
| `POSTGRES_PASSWORD` | Datenbank. Sieht niemand außer den Containern. |
| `Admin__Password` | **Damit meldest du dich am Panel an.** Das ist das Passwort, das du dir merken musst. |

Speichern: `Strg+O`, `Enter`, `Strg+X`.

```bash
chmod 600 .env
```

**Kontrolle** — muss `0` ausgeben:

```bash
grep -c 'bitte-aendern' .env
```

Steht dort `1` oder `2`, ist ein Vorgabewert stehengeblieben. Das Backend würde
starten, aber mit einem öffentlich bekannten Passwort.

---

## 4. Starten

```bash
docker compose -f docker-compose.yml up -d --build
```

> **Das `-f docker-compose.yml` gehört dazu.** Ohne die Angabe zieht Compose
> zusätzlich `docker-compose.override.yml` heran — die ist für die Entwicklung
> und veröffentlicht den Datenbank-Port.

Der erste Lauf baut beide Images und dauert einige Minuten.

```bash
docker compose -f docker-compose.yml ps
```

**Kontrolle** — `db` und `backend` müssen `healthy` zeigen:

```
NAME                      STATUS
soopworkshop-db           Up 2 minutes (healthy)
soopworkshop-backend-1    Up 1 minute (healthy)
soopworkshop-frontend-1   Up 1 minute
```

Die IP der VM:

```bash
hostname -I
```

**Kontrolle** — `http://<ip-der-vm>` von einem **anderen Rechner im Netz**
öffnen. Die Aufgabenübersicht muss erscheinen.

> Chrome und Edge schreiben „Nicht sicher" neben die Adresse. Das ist bei http
> immer so, ist ein Label und keine Warnseite — die Seite lädt sofort. Warum das
> so gewählt ist: [betrieb.md](betrieb.md#warum-kein-https).

---

## 5. Anmelden und Aufgaben anlegen

`http://<ip-der-vm>/admin` öffnen, mit `Admin__Password` aus der `.env`
anmelden.

**Hast du schon einen Aufgabenbestand als JSON-Datei:** *Transfer → Datei
wählen*. Die Vorschau zeigt vor dem Ausführen, was passieren würde. Fertig.

**Von Hand** — die Reihenfolge ist Pflicht:

1. Kategorie anlegen
2. Aufgabe anlegen (Titel, Beschreibung, Auswertungsmodus)
3. Testfälle bzw. JUnit-Dateien ergänzen
4. **Danach** sichtbar schalten

> Schritt 4 geht erst nach Schritt 3. Eine Aufgabe, deren Modus Daten verlangt,
> die es noch nicht gibt, lässt sich nicht sichtbar schalten — sonst würde sie
> still milder bewertet, weil die fehlende Kategorie aus der Wertung fällt.

**Kontrolle** — bei der Aufgabe auf *Probelauf*, eine Musterlösung hochladen.
Kommt die erwartete Punktzahl heraus, funktioniert die ganze Kette: Kompilieren,
Testfälle, JUnit.

**Damit läuft das System.**

---

## Wenn etwas nicht läuft

### `backend` wird nicht `healthy`

```bash
docker compose -f docker-compose.yml logs backend | tail -40
```

| Meldung | Ursache |
|---|---|
| „Es ist kein Admin-Passwort gesetzt" | `Admin__Password` fehlt in der `.env` |
| „Die Datenbank war beim Versuch N von 10 nicht bereit" | in den ersten Sekunden normal. Zehnmal → `logs db` ansehen |
| `28P01 password authentication failed` | siehe unten |

**Zu `28P01`:** `POSTGRES_PASSWORD` wirkt **nur beim ersten Anlegen** des
Datenvolumes. Später geändert? Dann behält die Datenbank ihr altes. Wenn noch
keine Daten drin sind:

```bash
docker compose -f docker-compose.yml down -v && docker compose -f docker-compose.yml up -d --build
```

> `down -v` **löscht die Datenbank.**

### 502 Bad Gateway

Das Backend ist noch nicht oben oder abgestürzt. `ps` und dann die Logs.

### Seite lädt, aber ist leer

Noch keine Aufgabe angelegt oder keine sichtbar geschaltet — Schritt 5.

### Jede Abgabe schlägt fehl

```bash
docker compose -f docker-compose.yml exec backend javac -version
```

Kommt nichts, ist das Image kaputt gebaut:
`docker compose -f docker-compose.yml build --no-cache backend`.

---

## Danach

> **Nicht ins Internet freigeben.** Keine Portfreigabe im Router, kein
> Tunneldienst. Jede Abgabe wird auf dieser VM **ausgeführt** — der Aufbau
> hindert diesen Code daran, ins Netz zu greifen, aber die VM selbst gehört als
> nicht vertrauenswürdig behandelt. Snapshot vor dem Workshop.

Alles Weitere in [docs/betrieb.md](betrieb.md): Firewall, Sicherung,
Aktualisieren, ein DNS-Name statt der IP, HTTPS nachrüsten.

---

## Kurzreferenz

```bash
docker compose -f docker-compose.yml up -d --build    # starten / aktualisieren
docker compose -f docker-compose.yml ps               # Zustand
docker compose -f docker-compose.yml logs -f backend  # Protokoll
docker compose -f docker-compose.yml down             # stoppen (Daten bleiben)
```

| | |
|---|---|
| Teilnehmer | `http://<ip-der-vm>` |
| Verwaltung | `http://<ip-der-vm>/admin` |
| Zugangsdaten | `.env` auf der VM |
