# Betrieb

Alles, was **nicht** gebraucht wird, damit das Tool läuft. Zum Aufsetzen siehe
[server-aufsetzen.md](server-aufsetzen.md).

| | |
|---|---|
| [Absichern](#absichern) | die einzige Maßnahme, die wirklich zählt |
| [Firewall](#firewall) | optional |
| [Sichern](#sichern) | vor jedem Aktualisieren |
| [Aktualisieren](#aktualisieren) | `git pull` plus neu bauen |
| [Ein Name statt der IP](#ein-name-statt-der-ip) | reiner Komfort |
| [Warum kein HTTPS](#warum-kein-https) | die Abwägung |
| [HTTPS nachrüsten](#https-nachrüsten) | wenn du das Wurzelzertifikat verteilen kannst |
| [Docker nachinstallieren](#docker-nachinstallieren) | falls das Compose-Plugin fehlt |

---

## Absichern

**Jede Abgabe wird ausgeführt.** Ein Teilnehmer schickt Java-Code, und der läuft
auf dieser VM. Das ist keine Unterstellung von Böswilligkeit — eine einzige
Zeile `new Socket(...)` genügt, auch versehentlich.

Was der Aufbau dagegen tut:

- **Backend und Datenbank liegen in einem Docker-Netz mit `internal: true`.** Der
  Container, in dem die Abgaben laufen, hat **keine Route ins LAN und keine ins
  Internet**. Er braucht auch keine: das JUnit-JAR liegt im Image, es wird nichts
  nachgeladen.
- **Grenzen für Speicher, CPU und Prozessanzahl** — der Unterschied zwischen
  einer Endlosschleife, die einen Container ausbremst, und einer, die die VM
  erlegt.
- **Zeitgrenzen** je Kompilierlauf und je Testlauf.

Was er **nicht** tut: eine Abgabe erreicht weiterhin die Datenbank und kann den
Container belasten. Echte Isolation je Abgabe (ein eigener Container pro
Auswertung) ist eine spätere Ausbaustufe.

**Daraus folgt:**

- **Nicht ins Internet freigeben.** Keine Portfreigabe im Router, kein
  Tunneldienst, kein Reverse Proxy von außen. Es gibt genau ein Passwort, keine
  Benutzerverwaltung, und der Verkehr läuft über http.
- Keine Firmen-Zugangsdaten auf der VM, keine SSH-Schlüssel zu anderen Systemen
- Wenn möglich in ein eigenes VLAN oder Gastnetz
- **Snapshot vor dem Workshop** — Zurücksetzen ist dann eine Sache von Sekunden

---

## Firewall

```bash
sudo apt-get install -y ufw && sudo ufw allow from 192.168.1.0/24 to any port 80 proto tcp && sudo ufw allow from 192.168.1.0/24 to any port 22 proto tcp && sudo ufw enable
```

Subnetz anpassen. Der Datenbank-Port ist gar nicht veröffentlicht — im
Betriebsaufbau erreicht ihn nur das Backend.

**Kontrolle** von einem anderen Rechner: `nmap -p 22,80,5432 <ip-der-vm>` —
`5432` muss `closed` oder `filtered` sein.

---

## Sichern

**Der Aufgabenbestand** — *Verwaltung → Transfer → Export* lädt alles als eine
JSON-Datei herunter (ohne Abgaben, das sind Workshop-Daten). Diese Datei gehört
auf deinen Rechner, nicht nur auf die VM. Das ist der Weg, den du am häufigsten
brauchst.

**Die ganze Datenbank:**

```bash
docker compose -f docker-compose.yml exec -T db pg_dump -U postgres soopworkshop > sicherung-$(date +%F).sql
```

Zurückspielen:

```bash
cat sicherung-2026-08-18.sql | docker compose -f docker-compose.yml exec -T db psql -U postgres soopworkshop
```

---

## Aktualisieren

```bash
git pull && docker compose -f docker-compose.yml up -d --build
```

Migrationen wendet das Backend beim Start selbst an. **Vorher sichern.**

Autostart nach einem Neustart der VM passiert von selbst — alle Dienste tragen
`restart: unless-stopped`.

> **Abgaben, die zum Zeitpunkt eines Neustarts liefen, stehen danach auf
> „Fehlgeschlagen"** — mit einem Hinweis für den Teilnehmer, dass er erneut
> einreichen soll. Das ist das dokumentierte Verhalten und kein Fehler: eine
> abgebrochene Auswertung lässt sich nicht fortsetzen.

---

## Ein Name statt der IP

Damit die Teilnehmer `http://soop.workshop` tippen statt einer IP. **Reiner
Komfort** — ohne Zertifikat hängt nichts daran, und am Server ist **nichts** zu
ändern: nginx antwortet auf jeden Namen.

Gebraucht wird ein **A-Record**: Name → IP der VM.

- **Vorhandener interner DNS** (Windows Server / AD, Pi-hole, OPNsense): dort
  eintragen lassen. Eine Bitte an die IT, keine Bastelei.
- **`hosts`-Datei** auf den Teilnehmerrechnern als Rückfall. Unter Windows als
  Administrator in `C:\Windows\System32\drivers\etc\hosts`:
  ```
  192.168.1.50    soop.workshop
  ```

**Kontrolle** vom Teilnehmerrechner: `nslookup soop.workshop` liefert die IP.

Zwei Stolpersteine:

- `.workshop` ist eine **echte öffentliche Top-Level-Domain**. Intern
  unproblematisch, aber ein Name darunter kann eines Tages mit einem realen
  Eintrag kollidieren. ICANN hat `.internal` für private Netze reserviert — wenn
  du frei wählen kannst, ist `soop.internal` die sauberere Wahl.
- **DNS über HTTPS (DoH)** im Browser umgeht den lokalen Resolver. Löst die Seite
  bei genau einem Teilnehmer nicht auf, ist fast immer das die Ursache. Dann DoH
  abschalten oder für diesen Rechner einen `hosts`-Eintrag setzen.

---

## Warum kein HTTPS

Weil es für einen internen Namen kein Zertifikat gibt, dem Browser von sich aus
trauen — und jede Alternative wäre für die Teilnehmer schlechter.

| | Was der Teilnehmer erlebt |
|---|---|
| **http (so wie hier)** | Seite lädt sofort. Kleines „Nicht sicher" neben der Adresse. **Nichts zu klicken.** |
| https selbstsigniert | **Ganzseitige Warnung** „Ihre Verbindung ist nicht privat" — auf jedem Rechner, bei jedem Besuch |
| https mit eigener CA | Schloss, keine Warnung — aber ein Wurzelzertifikat muss auf **jeden** Teilnehmerrechner |

Das „Nicht sicher" lässt sich nicht wegkonfigurieren; ein Schloss gibt es nur mit
einem vertrauenswürdigen Zertifikat. Es ist aber ein **Label, keine Warnseite** —
und damit deutlich harmloser als das, was ein selbstsigniertes Zertifikat
auslöst.

**Nebengewinn:** damit fällt DNS als Voraussetzung weg. Der einzige harte Grund
für einen Namen war das Zertifikat — ein Zertifikat gilt für einen Namen, nicht
für eine IP.

**Was das kostet, offen gesagt:** das Admin-Passwort läuft im Klartext durchs
LAN. In einem geschlossenen Kursnetz mit genau einem Betreuer ist das dieselbe
Risikoklasse wie das Passwort im Klartext in der `.env` auf dem Server. Deshalb
gehört dieser Aufbau nicht ins Internet.

---

## HTTPS nachrüsten

Nur sinnvoll, wenn du das Wurzelzertifikat auf die Teilnehmerrechner bringen
kannst — sonst tauschst du ein Label gegen eine Warnseite.

Am **Backend ist nichts zu ändern**: `X-Forwarded-Proto` wird bereits
ausgewertet, und die Cookie-Politik steht auf `SameAsRequest` — über https wird
das Anmelde-Cookie von selbst `Secure`.

1. Auf deinem Rechner `mkcert` installieren (`winget install
   FiloSottile.mkcert`), dann `mkcert -install`
2. `mkcert -cert-file soop.pem -key-file soop-key.pem soop.workshop <ip-der-vm>`
3. Beide Dateien auf die VM legen und in den Frontend-Container mounten
   (`./certs:/etc/nginx/certs:ro` unter `frontend.volumes`)
4. In `src/SoopWorkshop.Frontend/nginx.conf` einen zweiten `server`-Block mit
   `listen 443 ssl` und `ssl_certificate` ergänzen; Port `443:443` in
   `docker-compose.yml` veröffentlichen
5. `rootCA.pem` (Pfad: `mkcert -CAROOT`) auf jeden Teilnehmerrechner.
   **Firefox hat einen eigenen Zertifikatsspeicher** und ignoriert den von
   Windows
6. **Kein HSTS setzen.** Das würde die Zertifikatswarnung unumgehbar machen und
   jeden Rechner ohne Wurzelzertifikat komplett aussperren

Den privaten Schlüssel **nicht** ins Repository legen und **nicht** ins Image
backen — mounten.

---

## Docker nachinstallieren

Nur nötig, wenn `docker compose version` fehlschlägt.

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

```bash
sudo usermod -aG docker $USER
```

> Danach **einmal ab- und wieder anmelden.**
