# SoopWorkshop auf einer VM aufsetzen

Von der leeren Debian-VM zum laufenden System. **Fünf Schritte, rund 15 Minuten**
davon zehn Wartezeit beim Bauen.

Danach erreichen die Teilnehmer das Tool über `http://<ip-der-vm>` — ohne
Zertifikat, ohne DNS, ohne dass jemand auf einem Teilnehmerrechner etwas
einrichten muss. Die Seite lädt direkt, es gibt nichts wegzuklicken.

Alles Weitere — DNS-Name, Firewall, Sicherung — steht danach unter
[Optional](#optional) und ist für den Betrieb nicht nötig.

---

## 1. Docker prüfen

```bash
docker compose version
```

Kommt eine Version, weiter zu Schritt 2. Kommt ein Fehler, fehlt das
Compose-Plugin — dann [Docker nachinstallieren](#docker-nachinstallieren) unten.

Falls jeder Docker-Befehl „permission denied" sagt:

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

Zwei Passwörter würfeln lassen, nicht ausdenken:

```bash
openssl rand -base64 24
```

Zweimal aufrufen, dann eintragen:

```bash
nano .env
```

| Schlüssel | Wofür |
|---|---|
| `POSTGRES_PASSWORD` | Datenbank. Sieht niemand außer den Containern. |
| `Admin__Password` | **Damit meldest du dich am Panel an.** Das ist das Passwort, das du dir merken musst. |

Speichern mit `Strg+O`, `Enter`, `Strg+X`.

```bash
chmod 600 .env
```

**Kontrolle** — muss `0` ausgeben:

```bash
grep -c 'bitte-aendern' .env
```

Steht dort `1` oder `2`, ist ein Vorgabewert stehengeblieben. Das Backend würde
zwar starten, aber mit einem öffentlich bekannten Passwort.

---

## 4. Starten

```bash
docker compose -f docker-compose.yml up -d --build
```

> **Das `-f docker-compose.yml` gehört dazu.** Ohne die Angabe zieht Compose
> zusätzlich `docker-compose.override.yml` heran — die ist für die Entwicklung
> und veröffentlicht den Datenbank-Port.

Der erste Lauf baut beide Images und dauert einige Minuten. Danach:

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

Zeigt `backend` nach zwei Minuten nicht `healthy`, hilft
[Fehlersuche](#fehlersuche).

Die IP der VM:

```bash
hostname -I
```

**Kontrolle** — von einem **anderen Rechner im Netz** `http://<ip-der-vm>`
öffnen. Die Aufgabenübersicht muss erscheinen.

---

## 5. Anmelden und Aufgaben einspielen

`http://<ip-der-vm>/admin` öffnen, mit `Admin__Password` aus der `.env`
anmelden.

**Der schnelle Weg:** hast du auf deinem Rechner schon einen Aufgabenbestand,
dann *Transfer → Datei wählen*. Die Vorschau zeigt vor dem Ausführen, was
passieren würde.

**Von Hand** — die Reihenfolge ist Pflicht:

1. Kategorie anlegen
2. Aufgabe anlegen (Titel, Beschreibung, Auswertungsmodus)
3. Testfälle bzw. JUnit-Dateien ergänzen
4. **Danach** sichtbar schalten

> Schritt 4 geht erst nach Schritt 3. Eine Aufgabe, deren Modus Daten verlangt,
> die es noch nicht gibt, lässt sich nicht sichtbar schalten — sonst würde sie
> still milder bewertet, weil die fehlende Kategorie aus der Wertung fällt.

**Kontrolle** — im Panel bei der Aufgabe auf *Probelauf*, eine Musterlösung
hochladen. Kommt die erwartete Punktzahl heraus, funktioniert die ganze Kette:
Kompilieren, Testfälle, JUnit.

**Damit läuft das System.** Alles Weitere ist Komfort und Absicherung.

---

## Warum kein HTTPS

Kurz: weil es für einen internen Namen kein Zertifikat gibt, dem Browser von
sich aus trauen — und jede Alternative wäre für die Teilnehmer schlechter.

| | Was der Teilnehmer erlebt |
|---|---|
| **http (so wie hier)** | Seite lädt sofort. Kleines „Nicht sicher" neben der Adresse. **Nichts zu klicken.** |
| https selbstsigniert | **Ganzseitige Warnung** „Ihre Verbindung ist nicht privat" — auf jedem Rechner, bei jedem Besuch |
| https mit eigener CA | Schloss, keine Warnung — aber ein Wurzelzertifikat muss auf **jeden** Teilnehmerrechner |

Das „Nicht sicher" in der Adressleiste lässt sich nicht wegkonfigurieren; ein
Schloss gibt es nur mit einem vertrauenswürdigen Zertifikat. Es ist aber ein
**Label, keine Warnseite** — und damit deutlich harmloser als das, was ein
selbstsigniertes Zertifikat auslöst.

**Was das kostet, offen gesagt:** das Admin-Passwort läuft im Klartext durchs
LAN. In einem geschlossenen Kursnetz mit genau einem Betreuer ist das dieselbe
Risikoklasse wie das Passwort im Klartext in der `.env` auf dem Server. Deshalb
gehört dieser Aufbau auch nicht ins Internet — siehe
[Nicht nach außen freigeben](#nicht-nach-außen-freigeben).

**Nachrüsten ist später ein Konfigurationsschritt, keine Codeänderung:** das
Backend wertet `X-Forwarded-Proto` schon aus, und die Cookie-Politik steht auf
`SameAsRequest` — über https wird das Anmelde-Cookie von selbst `Secure`. Wie es
geht, steht unter [HTTPS nachrüsten](#https-nachrüsten).

---

## Optional

Nichts davon wird für den Betrieb gebraucht. In dieser Reihenfolge lohnt es:

### Nicht nach außen freigeben

**Das ist die einzige Absicherung, die wirklich zählt.** Keine Portfreigabe im
Router, kein Tunneldienst, kein Reverse Proxy von außen.

Grund: **jede Abgabe wird ausgeführt.** Ein Teilnehmer schickt Java-Code, und
der läuft auf dieser VM. Der Aufbau hält ihn davon ab, ins Netz zu greifen —
Backend und Datenbank liegen in einem Docker-Netz **ohne Route ins LAN und ohne
Route ins Internet** —, dazu Grenzen für Speicher, CPU und Prozesse sowie
Zeitgrenzen je Lauf.

Was er **nicht** tut: eine Abgabe erreicht weiterhin die Datenbank und kann den
Container belasten. Deshalb:

- Keine Firmen-Zugangsdaten auf der VM, keine SSH-Schlüssel zu anderen Systemen
- Wenn möglich in ein eigenes VLAN oder Gastnetz
- **Snapshot vor dem Workshop** — Zurücksetzen ist dann eine Sache von Sekunden

### Firewall

```bash
sudo apt-get install -y ufw && sudo ufw allow from 192.168.1.0/24 to any port 80 proto tcp && sudo ufw allow from 192.168.1.0/24 to any port 22 proto tcp && sudo ufw enable
```

Subnetz anpassen. Der Datenbank-Port ist gar nicht veröffentlicht.

### Ein Name statt der IP

Damit die Teilnehmer `http://soop.workshop` tippen statt einer IP. **Reiner
Komfort** — ohne Zertifikat hängt nichts daran.

Gebraucht wird ein **A-Record**: Name → IP der VM.

- **Vorhandener interner DNS** (Windows Server / AD, Pi-hole, OPNsense): dort
  eintragen lassen. Das ist eine Bitte an die IT, keine Bastelei.
- **`hosts`-Datei** auf den Teilnehmerrechnern als Rückfall. Unter Windows als
  Administrator in `C:\Windows\System32\drivers\etc\hosts`:
  ```
  192.168.1.50    soop.workshop
  ```

Es ist am Server **nichts** zu ändern — nginx antwortet auf jeden Namen.

**Kontrolle** vom Teilnehmerrechner: `nslookup soop.workshop` liefert die IP.

> Zwei Stolpersteine: `.workshop` ist eine echte öffentliche Top-Level-Domain —
> intern unproblematisch, aber `.internal` ist für private Netze reserviert und
> die sauberere Wahl. Und **DNS über HTTPS im Browser** umgeht den lokalen
> Resolver; löst die Seite bei genau einem Teilnehmer nicht auf, ist fast immer
> das die Ursache.

### Sichern

Der Aufgabenbestand: *Verwaltung → Transfer → Export* lädt alles als eine
JSON-Datei. **Die gehört auf deinen Rechner, nicht nur auf die VM.**

Die ganze Datenbank:

```bash
docker compose -f docker-compose.yml exec -T db pg_dump -U postgres soopworkshop > sicherung-$(date +%F).sql
```

Zurückspielen:

```bash
cat sicherung-2026-08-18.sql | docker compose -f docker-compose.yml exec -T db psql -U postgres soopworkshop
```

### Aktualisieren

```bash
git pull && docker compose -f docker-compose.yml up -d --build
```

Migrationen wendet das Backend beim Start selbst an. **Vorher sichern.**

Autostart nach einem Neustart der VM passiert von selbst — alle Dienste tragen
`restart: unless-stopped`.

### HTTPS nachrüsten

Nur sinnvoll, wenn du das Wurzelzertifikat auf die Teilnehmerrechner bringen
kannst (sonst tauschst du ein Label gegen eine Warnseite). In Kurzform:

1. Auf deinem Rechner `mkcert` installieren, `mkcert -install`, dann
   `mkcert -cert-file soop.pem -key-file soop-key.pem soop.workshop <ip>`
2. Beide Dateien auf die VM legen und in den Frontend-Container mounten
3. In `src/SoopWorkshop.Frontend/nginx.conf` einen `listen 443 ssl`-Block mit
   `ssl_certificate` ergänzen, Port 443 in `docker-compose.yml` veröffentlichen
4. `rootCA.pem` (Pfad: `mkcert -CAROOT`) auf jeden Teilnehmerrechner —
   **Firefox hat einen eigenen Zertifikatsspeicher**
5. **Kein HSTS setzen.** Das würde die Warnung unumgehbar machen und jeden
   Rechner ohne Wurzelzertifikat komplett aussperren

Am Backend ist **nichts** zu ändern: `X-Forwarded-Proto` wird ausgewertet, und
das Anmelde-Cookie wird über https von selbst `Secure`.

### Docker nachinstallieren

```bash
sudo apt-get update && sudo apt-get install -y ca-certificates curl gnupg
```

```bash
sudo install -m 0755 -d /etc/apt/keyrings && curl -fsSL https://download.docker.com/linux/debian/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg && sudo chmod a+r /etc/apt/keyrings/docker.gpg
```

```bash
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
```

```bash
sudo apt-get update && sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

---

## Fehlersuche

### Das Backend wird nicht `healthy`

```bash
docker compose -f docker-compose.yml logs backend | tail -40
```

- **„Es ist kein Admin-Passwort gesetzt"** → `Admin__Password` fehlt in der `.env`
- **„Die Datenbank war beim Versuch N von 10 nicht bereit"** → in den ersten
  Sekunden normal. Kommt es zehnmal, läuft die Datenbank nicht:
  `docker compose -f docker-compose.yml logs db`
- **`28P01 password authentication failed`** → `POSTGRES_PASSWORD` wirkt **nur
  beim ersten Anlegen** des Datenvolumes. Später geändert? Dann behält die
  Datenbank ihr altes. Wenn noch keine Daten drin sind:
  ```bash
  docker compose -f docker-compose.yml down -v && docker compose -f docker-compose.yml up -d --build
  ```
  > `down -v` **löscht die Datenbank.**

### Die Seite lädt, aber alles ist leer

Noch keine Aufgaben angelegt oder keine sichtbar geschaltet — Schritt 5.

### 502 Bad Gateway

Das Backend ist noch nicht oben oder gerade abgestürzt.
`docker compose -f docker-compose.yml ps` und dann die Logs.

### Jede Abgabe schlägt fehl

Prüfen, ob das JDK im Image steckt:

```bash
docker compose -f docker-compose.yml exec backend javac -version
```

Kommt nichts, ist das Image kaputt gebaut — neu bauen mit
`docker compose -f docker-compose.yml build --no-cache backend`.

### Abgaben stehen nach einem Neustart auf „Fehlgeschlagen"

**Kein Fehler, sondern Absicht.** Eine Auswertung, die beim Herunterfahren
abgebrochen wird, lässt sich nicht fortsetzen; beim nächsten Start werden solche
Abgaben markiert, mit einem Hinweis für den Teilnehmer. Er reicht neu ein.

### Umlaute erscheinen zerlegt

Sollte nicht vorkommen, die ganze Kette ist auf UTF-8 festgelegt und das ist
geprüft. Tritt es doch auf, gehört es gemeldet — mit Aufgabe, Abgabe und einem
Auszug aus `docker compose -f docker-compose.yml logs backend`.

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
