# SoopWorkshop auf einer VM aufsetzen

Von der leeren Debian-VM bis zum laufenden System, das die Teilnehmer unter
`https://soop.workshop` erreichen.

**Diese Anleitung wird von oben nach unten abgearbeitet.** Nach jedem Abschnitt
steht eine Kontrolle — woran du erkennst, dass der Schritt geklappt hat. Wenn
eine Kontrolle fehlschlägt, hilft Abschnitt 11 weiter, statt weiterzumachen.

> **Der Aufbau ist ausschließlich für das lokale Netz gedacht.** Nichts davon
> gehört ins Internet freigegeben — warum, steht in Abschnitt 9.

---

## Inhalt

| | |
|---|---|
| [1](#1-was-du-brauchst) | Was du brauchst |
| [2](#2-vm-vorbereiten) | VM vorbereiten |
| [3](#3-projekt-auf-die-vm) | Projekt auf die VM |
| [4](#4-env-anlegen) | `.env` anlegen |
| [5](#5-dns-einrichten) | DNS einrichten |
| [6](#6-zertifikat-erzeugen-und-verteilen) | Zertifikat erzeugen und verteilen |
| [7](#7-starten) | Starten |
| [8](#8-erste-anmeldung-und-aufgaben-einspielen) | Erste Anmeldung und Aufgaben einspielen |
| [9](#9-absichern) | Absichern |
| [10](#10-betrieb) | Betrieb |
| [11](#11-fehlersuche) | Fehlersuche |

---

## 1. Was du brauchst

**Die VM**

| | Richtwert | Warum |
|---|---|---|
| Betriebssystem | Debian 13 (oder 12) | alles läuft in Containern, die Distribution ist zweitrangig |
| CPU | 2 vCPU | jede Abgabe startet `javac` und eine JVM |
| RAM | 4 GB | 2 GB davon darf der Backend-Container nutzen |
| Platte | 20 GB | Images und Datenbank |
| Netz | **feste IP** | der DNS-Eintrag zeigt darauf; eine wechselnde Adresse macht ihn wertlos |

**Außerdem**

- Zugriff auf den DNS des Netzes — oder die Möglichkeit, auf den
  Teilnehmerrechnern die `hosts`-Datei zu ändern (Abschnitt 5)
- Deinen Arbeitsrechner (Windows), auf dem du das Zertifikat erzeugst
- Rechte, das Wurzelzertifikat auf die Teilnehmerrechner zu bringen

**Was du nicht brauchst:** .NET, Node oder ein JDK auf der VM. Alles davon
steckt in den Images.

---

## 2. VM vorbereiten

Docker Engine und das Compose-Plugin aus dem offiziellen Repository:

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

Damit du Docker ohne `sudo` benutzen kannst:

```bash
sudo usermod -aG docker $USER
```

> **Danach einmal ab- und wieder anmelden.** Gruppenzugehörigkeiten gelten erst
> in einer neuen Sitzung — sonst bekommst du bei jedem Befehl
> „permission denied while trying to connect to the Docker daemon socket".

Zeitzone setzen, damit die Zeitstempel der Abgaben lesbar sind:

```bash
sudo timedatectl set-timezone Europe/Berlin
```

**Kontrolle**

```bash
docker run --rm hello-world
```

Muss „Hello from Docker!" ausgeben — ohne `sudo`.

---

## 3. Projekt auf die VM

```bash
sudo apt-get install -y git && git clone https://github.com/Jermayn1/SoopWorkshop.git && cd SoopWorkshop
```

Hat die VM kein Internet, packst du das Repository auf deinem Arbeitsrechner ein
(`git archive`) und kopierst es mit `scp` herüber. Die Container werden aber aus
dem Internet gebaut — ohne Netz brauchst du fertige Images (Abschnitt 10,
„Ohne Internet auf der VM").

**Kontrolle**

```bash
ls docker-compose.yml .env.example
```

Beide Dateien müssen da sein.

---

## 4. `.env` anlegen

Die `.env` ist die einzige Stelle mit Zugangsdaten. Sie ist gitignoriert und
gehört **nicht** ins Repository.

```bash
cp .env.example .env
```

Zwei Passwörter erzeugen — nicht selbst ausdenken, sondern würfeln lassen:

```bash
openssl rand -base64 24
```

Zweimal aufrufen und die Werte eintragen:

```bash
nano .env
```

| Schlüssel | Bedeutung |
|---|---|
| `POSTGRES_PASSWORD` | Passwort der Datenbank. Sieht niemand außer den Containern. |
| `Admin__Password` | **Damit meldest du dich am Panel an.** Das ist das Passwort, das du dir merken musst. |

> **`Admin__Password` ist Pflicht.** Fehlt es, startet das Backend nicht — mit
> einer Meldung, die genau das sagt. Ein stiller Start ohne Zugangsschutz wäre
> schlimmer als ein Abbruch.

Rechte einschränken, damit die Datei nicht jeder auf der VM lesen kann:

```bash
chmod 600 .env
```

**Kontrolle**

```bash
grep -c 'bitte-aendern' .env
```

Muss `0` ausgeben. Steht dort noch eine `1` oder `2`, ist mindestens ein
Vorgabewert stehengeblieben.

---

## 5. DNS einrichten

**Warum überhaupt ein Name?** Zwei Gründe, und der zweite wiegt schwerer:

1. Teilnehmer tippen `soop.workshop` statt `192.168.x.y`.
2. **Das Zertifikat lautet auf den Namen, nicht auf die IP.** Wer die IP
   eintippt, bekommt trotz gültigem Zertifikat eine Warnung. Der Name muss
   deshalb *vor* Abschnitt 6 stehen.

Ein DNS-Eintrag ist eine Zuordnung von Name zu Adresse — ein **A-Record**:

```
soop.workshop.  →  192.168.1.50
```

Erst die IP der VM feststellen:

```bash
hostname -I
```

Dann einen der drei Wege:

### Weg 1 — der vorhandene interne DNS *(der Normalfall)*

Betreibt die Ausbildung einen internen DNS-Server (Windows Server / Active
Directory, Pi-hole, OPNsense, Fritz!Box mit eigenen Einträgen), gehört der
A-Record dorthin. Das ist eine Bitte an die IT und keine Bastelei — genau dafür
ist der Server da.

Was du melden musst: **Name `soop.workshop`, Typ `A`, Ziel = die IP von oben.**

### Weg 2 — dnsmasq auf derselben VM

Wenn es keinen internen DNS gibt:

```bash
sudo apt-get install -y dnsmasq
```

```bash
echo "address=/soop.workshop/$(hostname -I | awk '{print $1}')" | sudo tee /etc/dnsmasq.d/soop.conf
```

```bash
sudo systemctl restart dnsmasq
```

Danach muss im DHCP des Routers die VM als DNS-Server eingetragen werden, damit
die Teilnehmerrechner sie überhaupt fragen.

> **Bedenke:** damit hängt die Namensauflösung *aller* Rechner an dieser VM. Ist
> sie aus, kommt niemand mehr ins Internet. Für einen Workshop-Tag vertretbar,
> als Dauerlösung nicht.

### Weg 3 — `hosts`-Datei *(Rückfall)*

Ohne jede Infrastruktur, auf **jedem** Teilnehmerrechner. Unter Windows als
Administrator in `C:\Windows\System32\drivers\etc\hosts`:

```
192.168.1.50    soop.workshop
```

Funktioniert immer, skaliert schlecht — bei 15 Rechnern ist das 15-mal Arbeit.

### Anmerkung zum Namen

`.workshop` ist eine **echte öffentliche Top-Level-Domain**. Intern ist das
unproblematisch, aber ein Name darunter kann eines Tages mit einem realen
Eintrag kollidieren. ICANN hat `.internal` genau für private Netze reserviert —
wenn du frei wählen kannst, ist `soop.internal` die sauberere Wahl. Ändern musst
du dafür nur den DNS-Eintrag und den Namen im Zertifikat; die Anwendung kennt
ihren eigenen Namen nicht.

**Kontrolle** — von einem **Teilnehmerrechner** aus, nicht von der VM:

```
nslookup soop.workshop
```

Muss die IP der VM liefern.

---

## 6. Zertifikat erzeugen und verteilen

Für einen internen Namen gibt es kein öffentlich vertrauenswürdiges Zertifikat.
Du wirst also deine eigene kleine Zertifizierungsstelle (CA), und deren
Wurzelzertifikat kommt einmalig auf die Teilnehmerrechner. Danach zeigt der
Browser ein Schloss und **keine Warnung**.

> **Warum nicht einfach selbstsigniert?** Weil dann jeder Browser bei jedem
> Besuch eine ganzseitige Warnung zeigt. Das ist nicht nur unschön: es bringt
> einem ganzen Kurs bei, Sicherheitswarnungen wegzuklicken.

### 6.1 mkcert auf deinem Arbeitsrechner

```powershell
winget install FiloSottile.mkcert
```

Einmalig die eigene CA anlegen und im Windows-Zertifikatsspeicher eintragen:

```powershell
mkcert -install
```

### 6.2 Zertifikat erzeugen

Das Skript im Repository kapselt den Aufruf, damit die Erneuerung in einem Jahr
kein Rätselraten wird:

```powershell
.\scripts\erzeuge-zertifikat.ps1 -Name soop.workshop -ServerIp 192.168.1.50
```

Es legt `certs\soop.pem` und `certs\soop-key.pem` an und nennt am Ende den Pfad
zum Wurzelzertifikat.

### 6.3 Auf die VM kopieren

```powershell
scp certs\soop.pem certs\soop-key.pem benutzer@192.168.1.50:~/SoopWorkshop/certs/
```

Auf der VM die Rechte des Schlüssels einschränken:

```bash
chmod 600 certs/soop-key.pem
```

### 6.4 Wurzelzertifikat auf die Teilnehmerrechner

Den Pfad nennt:

```powershell
mkcert -CAROOT
```

Aus diesem Ordner die Datei `rootCA.pem` verteilen. Auf einem Windows-Rechner
als Administrator:

```powershell
Import-Certificate -FilePath rootCA.pem -CertStoreLocation Cert:\LocalMachine\Root
```

> **Firefox hat einen eigenen Zertifikatsspeicher** und ignoriert den von
> Windows. Entweder in Firefox unter *Einstellungen → Datenschutz und Sicherheit
> → Zertifikate anzeigen → Importieren* nachziehen, oder
> `security.enterprise_roots.enabled` auf `true` setzen. Chrome und Edge
> benutzen den Windows-Speicher.

**Wer das Wurzelzertifikat nicht installiert, kommt trotzdem hinein** — mit
einer Warnung, die sich wegklicken lässt. Das ist der dokumentierte Rückfall und
kein Fehler. (Deshalb setzt der Server bewusst **kein HSTS**: das würde die
Warnung unumgehbar machen und solche Rechner komplett aussperren.)

**Kontrolle**

```bash
ls -l certs/soop.pem certs/soop-key.pem
```

Beide Dateien müssen auf der VM liegen.

---

## 7. Starten

```bash
docker compose -f docker-compose.yml up -d --build
```

> **Das `-f docker-compose.yml` ist Absicht.** Ohne die Angabe zieht Compose
> zusätzlich `docker-compose.override.yml` heran — die ist für die Entwicklung
> und veröffentlicht den Datenbank-Port nach außen.

Der erste Lauf baut beide Images und dauert einige Minuten. Danach:

```bash
docker compose -f docker-compose.yml ps
```

**Kontrolle** — `db` und `backend` müssen `healthy` zeigen, `frontend` `Up`:

```
NAME                    STATUS
soopworkshop-db         Up 2 minutes (healthy)
soopworkshop-backend-1  Up 1 minute (healthy)
soopworkshop-frontend-1 Up 1 minute
```

Was wirklich gilt, steht in der ersten Logzeile des Backends:

```bash
docker compose -f docker-compose.yml logs backend | head -30
```

Dort müssen zwei Dinge stehen:

```
Konfiguration: Datenbank db:5432/soopworkshop, Auswertung 4 gleichzeitig, ...
8 ausstehende Migration(en) werden angewendet: ...
```

Und im Frontend-Log **darf nicht** stehen, dass ein selbstsigniertes Zertifikat
erzeugt wurde:

```bash
docker compose -f docker-compose.yml logs frontend | grep -i achtung
```

Kommt hier eine Meldung, liegt das Zertifikat nicht am richtigen Ort — zurück zu
Abschnitt 6.3.

Jetzt vom **Teilnehmerrechner** aus `https://soop.workshop` öffnen. Es muss die
Aufgabenübersicht erscheinen, mit Schloss und ohne Warnung.

---

## 8. Erste Anmeldung und Aufgaben einspielen

`https://soop.workshop/admin` öffnen und mit `Admin__Password` aus der `.env`
anmelden.

**Entweder** von Hand anlegen — die Reihenfolge ist Pflicht:

1. Kategorie anlegen (Name, Symbol, Reihenfolge)
2. Aufgabe anlegen (Titel, Beschreibung, Schwierigkeit, Auswertungsmodus)
3. Testfälle bzw. JUnit-Dateien ergänzen
4. **Danach** sichtbar schalten

> Schritt 4 geht erst nach Schritt 3. Eine Aufgabe, deren Auswertungsmodus Daten
> verlangt, die es noch nicht gibt, lässt sich nicht sichtbar schalten — sonst
> würde sie still milder bewertet, weil die fehlende Kategorie aus der Wertung
> fällt.

**Oder** einen fertigen Bestand einspielen: *Verwaltung → Transfer → Datei
wählen*. Die Vorschau zeigt vor dem Ausführen, was passieren würde. Damit kannst
du den Aufgabenbestand auf deinem Rechner vorbereiten und hier nur einspielen.

**Kontrolle**

Aufgabe im Panel unter *Vorschau* ansehen (zeigt exakt die Teilnehmersicht),
dann eine Musterlösung über *Probelauf* hochladen. Kommt die erwartete
Punktzahl heraus, funktioniert die ganze Kette — Kompilieren, Testfälle, JUnit.

> Ein Probelauf erzeugt eine **echte** Abgabe. Sie taucht später in der
> Abgaben-Übersicht auf.

---

## 9. Absichern

### Das Wichtigste zuerst

**Jede Abgabe wird ausgeführt.** Ein Teilnehmer schickt Java-Code, und dieser
Code läuft auf deiner VM. Das ist keine Unterstellung von Böswilligkeit — eine
einzige Zeile `new Socket(...)` genügt, auch versehentlich.

Was der Aufbau dagegen tut:

- **Backend und Datenbank liegen in einem Docker-Netz mit `internal: true`.**
  Der Container, in dem die Abgaben laufen, hat **keine Route ins LAN und keine
  ins Internet**. Er braucht auch keine: das JUnit-JAR liegt im Image.
- **Grenzen für Speicher, CPU und Prozessanzahl** — der Unterschied zwischen
  einer Endlosschleife, die einen Container ausbremst, und einer, die die VM
  erlegt.
- **Zeitgrenzen** je Kompilierlauf und je Testlauf.

Was er **nicht** tut: eine Abgabe erreicht weiterhin die Datenbank und kann den
Container belasten. Echte Isolation je Abgabe (ein eigener Container pro
Auswertung) ist eine spätere Ausbaustufe.

**Daraus folgt: behandle die VM als nicht vertrauenswürdig.**

- Keine Firmen-Zugangsdaten darauf, kein SSH-Schlüssel zu anderen Systemen
- Wenn möglich in ein eigenes VLAN oder Gastnetz
- **Snapshot vor dem Workshop** — danach ist Zurücksetzen eine Sache von Sekunden

### Firewall

```bash
sudo apt-get install -y ufw
```

```bash
sudo ufw allow from 192.168.1.0/24 to any port 80 proto tcp
```

```bash
sudo ufw allow from 192.168.1.0/24 to any port 443 proto tcp
```

```bash
sudo ufw allow from 192.168.1.0/24 to any port 22 proto tcp
```

```bash
sudo ufw enable
```

Das Subnetz an dein Netz anpassen. Der Datenbank-Port ist gar nicht erst
veröffentlicht — im Betriebsaufbau erreicht ihn nur das Backend.

### Nicht ins Internet freigeben

**Keine Portfreigabe im Router, kein Reverse Proxy von außen, kein
Tunneldienst.** Das System ist workshop-intern gedacht:

- Es gibt genau ein Passwort und keine Benutzerverwaltung
- Es ist nicht gegen böswillige Abgaben gehärtet
- Die Zertifikate stammen aus deiner eigenen CA, der nur dein Netz vertraut

**Kontrolle** — von einem anderen Rechner:

```bash
nmap -p 22,80,443,5432 192.168.1.50
```

`5432` muss `closed` oder `filtered` sein.

---

## 10. Betrieb

### Autostart nach Neustart der VM

Passiert von selbst: alle Dienste tragen `restart: unless-stopped`, und Docker
startet mit dem System.

**Kontrolle:** VM neu starten, warten, `docker compose -f docker-compose.yml ps`
— alles muss wieder `healthy` sein.

### Sichern

Die Aufgaben liegen in der Datenbank. Zwei Wege, und du willst beide:

**Der schnelle für den Aufgabenbestand** — *Verwaltung → Transfer → Export*
lädt alles als eine JSON-Datei herunter (ohne Abgaben, das sind
Workshop-Daten). Diese Datei gehört auf deinen Rechner, nicht nur auf die VM.

**Der vollständige für die ganze Datenbank:**

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

### Zertifikat erneuern

mkcert-Zertifikate laufen nach gut zwei Jahren ab. Dann Abschnitt 6.2 und 6.3
wiederholen und neu starten:

```bash
docker compose -f docker-compose.yml restart frontend
```

Das Wurzelzertifikat auf den Clients bleibt gültig — es muss **nicht** neu
verteilt werden.

### Ohne Internet auf der VM

Images auf deinem Rechner bauen, als Archiv kopieren, dort laden:

```powershell
docker save soopworkshop-backend soopworkshop-frontend postgres:16-alpine -o soop-images.tar
```

```bash
docker load -i soop-images.tar
```

### Logs

```bash
docker compose -f docker-compose.yml logs -f backend
```

---

## 11. Fehlersuche

### Die Seite ist bei genau einem Teilnehmer nicht erreichbar

Fast immer **DNS über HTTPS (DoH)** im Browser: Firefox und Chrome fragen dann
einen Resolver im Internet statt den im Netz, und der kennt `soop.workshop`
nicht. Abschalten (Firefox: *Einstellungen → Verbindungs-Einstellungen → DNS
über HTTPS aus*) oder für diesen Rechner einen `hosts`-Eintrag setzen.

Gegenprobe: `nslookup soop.workshop` funktioniert (das benutzt DoH nicht), der
Browser aber nicht.

### Zertifikatswarnung trotz installiertem Wurzelzertifikat

- **Die IP statt des Namens aufgerufen?** Das Zertifikat lautet auf den Namen.
- **Firefox?** Eigener Zertifikatsspeicher, siehe Abschnitt 6.4.
- **Steht im Frontend-Log eine ACHTUNG-Meldung?** Dann benutzt der Container ein
  selbstsigniertes Notfall-Zertifikat, weil es deins nicht gefunden hat.
  Abschnitt 6.3 prüfen und `docker compose -f docker-compose.yml restart frontend`.

### `28P01 password authentication failed`

`POSTGRES_PASSWORD` wirkt **nur beim ersten Anlegen** des Datenvolumes. Wurde es
später geändert, behält die Datenbank ihr altes Passwort. Entweder das Passwort
in der Datenbank ändern oder — wenn noch keine Daten drin sind:

```bash
docker compose -f docker-compose.yml down -v && docker compose -f docker-compose.yml up -d --build
```

> `down -v` **löscht die Datenbank**. Vorher sichern.

### Das Backend wird nicht `healthy`

```bash
docker compose -f docker-compose.yml logs backend | tail -40
```

- „Es ist kein Admin-Passwort gesetzt" → `Admin__Password` fehlt in der `.env`
- „Die Datenbank war beim Versuch N von 10 nicht bereit" → normal in den ersten
  Sekunden; kommt es zehnmal, läuft die Datenbank nicht

### Jede Abgabe schlägt fehl

Prüfen, ob das JDK im Image ist:

```bash
docker compose -f docker-compose.yml exec backend javac -version
```

Kommt hier nichts, ist das Image kaputt gebaut — neu bauen mit `--no-cache`.

### Abgaben stehen nach einem Neustart auf „Fehlgeschlagen"

**Kein Fehler, sondern Absicht.** Eine Auswertung, die beim Herunterfahren
abgebrochen wird, kann nicht fortgesetzt werden; beim nächsten Start werden
solche Abgaben als fehlgeschlagen markiert, mit einem Hinweis für den
Teilnehmer. Er reicht einfach neu ein.

### Umlaute erscheinen zerlegt

Sollte nicht vorkommen — die ganze Kette ist auf UTF-8 festgelegt und das ist
geprüft. Tritt es doch auf, gehört es gemeldet: mit der Aufgabe, der Abgabe und
einem Auszug aus `docker compose logs backend`.

---

## Kurzreferenz

```bash
docker compose -f docker-compose.yml up -d --build    # starten / aktualisieren
docker compose -f docker-compose.yml ps               # Zustand
docker compose -f docker-compose.yml logs -f backend  # Protokoll
docker compose -f docker-compose.yml restart frontend # nur den Proxy neu
docker compose -f docker-compose.yml down             # stoppen (Daten bleiben)
```

| | |
|---|---|
| Teilnehmer | `https://soop.workshop` |
| Verwaltung | `https://soop.workshop/admin` |
| Zugangsdaten | `.env` auf der VM |
| Zertifikate | `certs/` auf der VM |
