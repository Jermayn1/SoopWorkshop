# Aufsetzen

Debian-VM mit Docker. Fünf Befehle, rund 15 Minuten (davon zehn Bauzeit).

Danach: `http://<ip-der-vm>` — kein Zertifikat, kein DNS, auf den
Teilnehmergeräten ist nichts einzurichten.

---

## 1. Docker prüfen

```bash
docker compose version
```

Fehler? → [Docker nachinstallieren](betrieb.md#docker-nachinstallieren)

„permission denied"? → `sudo usermod -aG docker $USER`, dann **ab- und wieder
anmelden**.

## 2. Projekt holen

```bash
sudo apt-get update && sudo apt-get install -y git && git clone https://github.com/Jermayn1/SoopWorkshop.git && cd SoopWorkshop
```

## 3. Passwörter setzen

```bash
cp .env.example .env && openssl rand -base64 24 && openssl rand -base64 24
```

Die zwei ausgegebenen Werte eintragen:

```bash
nano .env
```

| Schlüssel | Wofür |
|---|---|
| `POSTGRES_PASSWORD` | Datenbank, sieht nur der Container |
| `Admin__Password` | **damit meldest du dich am Panel an** |

Speichern: `Strg+O`, `Enter`, `Strg+X`. Dann:

```bash
chmod 600 .env && grep -c 'bitte-aendern' .env
```

Muss `0` ausgeben.

## 4. Starten

```bash
docker compose -f docker-compose.yml up -d --build
```

> Das `-f docker-compose.yml` gehört dazu — sonst lädt Compose die
> Entwicklungs-Override mit.

## 5. Prüfen

```bash
./scripts/pruefe-betrieb.sh
```

Prüft Docker, `.env`, alle drei Container, Erreichbarkeit und das JDK — und
nennt bei einem Fehlschlag den nächsten Schritt. Am Ende steht die Adresse für
die Teilnehmer.

---

## Aufgaben anlegen

`http://<ip-der-vm>/admin`, anmelden mit `Admin__Password`.

**Fertiger Bestand:** *Transfer → Datei wählen*. Fertig.

**Von Hand**, Reihenfolge ist Pflicht:

1. Kategorie → 2. Aufgabe → 3. Testfälle bzw. JUnit-Dateien → 4. **danach**
sichtbar schalten

> Schritt 4 geht erst nach Schritt 3, sonst würde die Aufgabe still milder
> bewertet.

**Prüfen:** *Probelauf* mit einer Musterlösung. Erwartete Punktzahl → die ganze
Kette läuft.

---

## Ports

| Port | Wo | Wofür | Von außen |
|---|---|---|---|
| **80** | VM | Weboberfläche und API | **ja** — der einzige, der offen sein muss |
| 8080 | Container | Backend | nein, nur im Docker-Netz |
| 5432 | Container | PostgreSQL | nein, nicht veröffentlicht |

**Eingehend freizugeben: nur Port 80/TCP aus dem Teilnehmernetz.**

Port 80 belegt? In der `.env`:

```
HTTP_PORT=8080
```

Dann läuft die Seite auf `http://<ip-der-vm>:8080`.

**Ausgehend** braucht die VM nur beim Bauen und Aktualisieren Internet (443 für
Docker-Images, NuGet, npm; git). Im Betrieb nicht — der Backend-Container hat
ohnehin keine Route nach draußen.

---

## Wenn etwas klemmt

Erst `./scripts/pruefe-betrieb.sh`. Sagt der nichts Brauchbares:

```bash
docker compose -f docker-compose.yml logs --tail=60 backend
```

| Symptom | Ursache |
|---|---|
| **502 Bad Gateway** | Backend unten. Logs ansehen. |
| „kein Admin-Passwort gesetzt" | `Admin__Password` fehlt in der `.env` |
| `28P01 password authentication failed` | sollte sich beim Start selbst beheben — die Datenbank gleicht ihr Passwort an die `.env` an. Bleibt es: `docker compose -f docker-compose.yml logs db \| grep angeglichen` |
| Seite leer | keine Aufgabe sichtbar geschaltet |
| `.env`-Werte wirken nicht | Windows-Zeilenenden: `sed -i 's/\r$//' .env` |
| jede Abgabe scheitert | `docker compose -f docker-compose.yml build --no-cache backend` |

---

## Befehle

```bash
docker compose -f docker-compose.yml up -d --build    # starten / aktualisieren
docker compose -f docker-compose.yml ps               # Zustand
docker compose -f docker-compose.yml logs -f backend  # Protokoll
docker compose -f docker-compose.yml down             # stoppen (Daten bleiben)
./scripts/pruefe-betrieb.sh                           # Betrieb prüfen
```

---

> **Nicht ins Internet freigeben.** Jede Abgabe wird auf dieser VM
> **ausgeführt**. Der Container dafür hat keine Route ins LAN und keine ins
> Internet, aber die VM selbst gehört als nicht vertrauenswürdig behandelt —
> Snapshot vor dem Workshop.

Absicherung, Sicherung, DNS-Name, HTTPS: [betrieb.md](betrieb.md)
