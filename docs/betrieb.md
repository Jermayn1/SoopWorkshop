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

## Das Schloss im Browser — der einzige Weg, der wirklich funktioniert

Wenn dich das „Nicht sicher" in der Adressleiste stört, gibt es dafür bei
**eigenen Geräten der Teilnehmer** genau eine Lösung: **eine Domain, die dir
gehört.**

Warum die Alternativen ausfallen:

- **Eigene CA** — setzt voraus, dass jemand auf jedes Gerät ein Wurzelzertifikat
  bringt. Bei Firmenrechnern in einer Domäne geht das per Gruppenrichtlinie; bei
  **privaten Geräten** ist es weder machbar noch angebracht. Man verlangt von
  niemandem, auf seinem eigenen Handy einer fremden CA zu vertrauen.
- **Selbstsigniert** — ganzseitige Warnung statt eines Labels. Schlechter als
  http.

### Wie es mit eigener Domain geht

Beispiel `soop.workshop` (eine echte, registrierbare Top-Level-Domain, rund
20–30 € im Jahr):

1. **Domain registrieren** bei einem Anbieter, dessen DNS eine API hat
   (Cloudflare, Hetzner, INWX, deSEC — deSEC ist kostenlos)
2. **A-Record auf die interne IP der VM** setzen, z. B.
   `soop.workshop → 192.168.1.50`. Das ist erlaubt: ein öffentlicher Name darf
   auf eine private Adresse zeigen.
3. **Zertifikat über die DNS-Challenge** holen — `certbot` mit dem Plugin des
   Anbieters. Die VM muss dafür **nicht von außen erreichbar sein**; sie weist
   sich über einen TXT-Eintrag aus.
4. Zertifikat in den Frontend-Container mounten und einen `listen 443
   ssl`-Block ergänzen (siehe unten), Port 443 veröffentlichen.
5. Erneuerung läuft per Cron von selbst — die VM braucht dafür ausgehend
   Internet, das hat sie.

**Ergebnis:** jedes Gerät, auch ein privates Handy, zeigt ein Schloss. Niemand
installiert etwas. Der Name funktioniert auch im Gastnetz, weil öffentliches DNS
befragt wird — ein *interner* DNS-Eintrag würde dort vermutlich gar nicht
gelten.

**Aufwand:** einmalig ein bis zwei Stunden, danach wartungsfrei.

**Der eine Haken:** manche Router und Resolver blockieren aus Sicherheitsgründen
Antworten, die einen öffentlichen Namen auf eine private IP abbilden
(„DNS-Rebind-Schutz", bei FRITZ!Box standardmäßig an). Das lässt sich
konfigurieren, gehört aber vorher geprüft — einmal `nslookup soop.workshop` von
einem Gerät im Gastnetz.

---

## HTTPS nachrüsten

Die technischen Schritte, unabhängig davon, woher das Zertifikat kommt.

Am **Backend ist nichts zu ändern**: `X-Forwarded-Proto` wird bereits
ausgewertet, und die Cookie-Politik steht auf `SameAsRequest` — über https wird
das Anmelde-Cookie von selbst `Secure`.

**Am Server, egal woher das Zertifikat stammt:**

1. Zertifikat und Schlüssel auf die VM legen und in den Frontend-Container
   mounten (`./certs:/etc/nginx/certs:ro` unter `frontend.volumes`)
2. In `src/SoopWorkshop.Frontend/nginx.conf` einen zweiten `server`-Block mit
   `listen 443 ssl` und `ssl_certificate` ergänzen
3. Port `443:443` in `docker-compose.yml` veröffentlichen
4. Optional den Port-80-Block auf `return 301 https://$host$request_uri;` stellen

**Nur bei eigener CA** (Firmenrechner in einer Domäne) kommen dazu:

5. Zertifikat erzeugen: `mkcert -install`, dann
   `mkcert -cert-file soop.pem -key-file soop-key.pem <name> <ip-der-vm>`
6. `rootCA.pem` (Pfad: `mkcert -CAROOT`) auf jeden Rechner verteilen —
   per Gruppenrichtlinie. **Firefox hat einen eigenen Zertifikatsspeicher**
   und ignoriert den von Windows
7. **Kein HSTS setzen.** Das würde die Zertifikatswarnung unumgehbar machen und
   jeden Rechner ohne Wurzelzertifikat komplett aussperren

Mit einem **öffentlichen Zertifikat** (Let's Encrypt, siehe oben) entfallen die
Schritte 5–7 vollständig — und HSTS wäre dann sogar unbedenklich.

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
