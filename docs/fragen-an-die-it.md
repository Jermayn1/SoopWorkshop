# Fragen an die IT

Gesprächsvorbereitung für den Betrieb von SoopWorkshop im Ausbildungsnetz.

Die Antworten entscheiden, **was wir bauen** — nicht nur, wie wir es
konfigurieren. Deshalb steht hinter jeder Frage, was sie auslöst.

---

## Zuerst: was du erzählen solltest

Kurz und ehrlich, damit sie einordnen können, worum es geht:

> „Ich habe für den SOOP-Workshop ein Tool gebaut, das Java-Abgaben der
> Teilnehmer automatisch auswertet. Es läuft komplett in Docker auf einer VM —
> Datenbank, Backend, Weboberfläche. Die Teilnehmer rufen im Browser eine Seite
> auf, laden ihre `.java`-Dateien hoch und bekommen eine Bewertung zurück.
>
> Zwei Dinge, die ihr wissen solltet:
>
> **Das Tool kompiliert und führt den Code der Teilnehmer aus.** Das ist sein
> Zweck. Der Container, in dem das passiert, hat weder eine Route ins LAN noch
> ins Internet, dazu Grenzen für Speicher, CPU und Prozesse. Trotzdem würde ich
> die VM als nicht vertrauenswürdig behandeln — am liebsten ein eigenes Segment
> und ein Snapshot vor dem Workshop.
>
> **Es ist rein intern.** Keine Freigabe nach außen, ein einziges Passwort für
> die Verwaltung, keine Benutzerkonten."

Das nimmt die zwei Fragen vorweg, die sie sonst selbst stellen — und es zeigt,
dass du das Risiko kennst.

---

## Die vier Fragen, die alles entscheiden

Wenn nur fünf Minuten Zeit sind, dann diese.

### 1. Gibt es eine interne Zertifizierungsstelle? Sind die Rechner in der Domäne?

**Die wichtigste Frage überhaupt.** Formulierung:

> „Habt ihr eine interne CA — etwa Active Directory Certificate Services? Und
> sind die Schulungsrechner in der Domäne?"

| Antwort | Was das bedeutet |
|---|---|
| **Ja, interne CA + Domänenrechner** | **Jackpot.** Sie stellen ein Zertifikat für unseren Namen aus, und **jeder Domänenrechner vertraut ihm bereits** — kein Aufwand auf den Clients, perfektes Schloss im Browser. Wir brauchen nur die Zertifikatsdatei und den Schlüssel. |
| Keine CA, aber Domäne + Gruppenrichtlinien | Fast so gut: ich erzeuge das Zertifikat, die IT verteilt das Wurzelzertifikat einmal per GPO. |
| Keine Domäne | Dann bleibt es bei http. Wurzelzertifikat auf 15 Rechner einzeln lohnt nicht. |

**Falls ja, gleich nachfragen:**
- Auf welchen Namen kann das Zertifikat lauten?
- Wie lange dauert die Ausstellung — Minuten oder Tage?
- Bekomme ich Zertifikat **und privaten Schlüssel** als Dateien (PEM oder PFX)?

### 2. Gibt es einen internen DNS-Server, und kann ich einen Eintrag bekommen?

> „Kann ich einen A-Record auf die IP der VM bekommen — etwa `soop` unter eurem
> internen Suffix?"

| Antwort | Was das bedeutet |
|---|---|
| Ja | Teilnehmer tippen einen Namen statt einer IP. Voraussetzung für Frage 1. |
| Nein | Läuft trotzdem — über die IP. Ohne Zertifikat hängt daran nichts. |

**Nachfragen:** Welches interne Suffix ist üblich? (Das gehört in den Namen im
Zertifikat, beides muss zusammenpassen.)

### 3. Bekomme ich eine VM — und hat sie Internetzugang?

> „Eine kleine Linux-VM, Debian, 2 vCPU / 4 GB / 20 GB, mit sudo für mich."

**Der Internetzugang ist der Punkt, der leicht untergeht.** Beim ersten Start
lädt Docker Basis-Images, .NET-Pakete und npm-Pakete. Ohne Internet muss ich die
Images vorher auf meinem Rechner bauen und als Datei mitbringen — machbar, aber
ich muss es **vorher** wissen.

| Antwort | Was das bedeutet |
|---|---|
| Internet direkt | Standardweg, nichts zu tun |
| Nur über Proxy | Sagen lassen: Adresse und Port. Docker und npm brauchen die Proxy-Einstellung |
| Kein Internet | Ich baue die Images zu Hause und bringe sie mit (`docker save`) |

### 4. Erreichen die Teilnehmerrechner die VM?

> „Liegen die Schulungsrechner im selben Netz wie die VM? Gibt es Client-Isolation
> im WLAN oder eine Firewall dazwischen?"

Klingt trivial, ist aber der häufigste Grund, warum am Workshoptag nichts geht.
**Client-Isolation im WLAN** ist der Klassiker: jeder kommt ins Internet, aber
keiner erreicht ein Gerät im selben Netz.

Wenn möglich: **einmal vorher ausprobieren**, von einem echten Schulungsrechner.

---

## Vollständige Liste

### VM

- [ ] Bekomme ich eine VM, oder gibt es schon eine? Debian oder Ubuntu?
- [ ] Ausstattung: 2 vCPU, 4 GB RAM, 20 GB Platte reichen
- [ ] Habe ich `sudo`?
- [ ] **Feste IP** oder DHCP-Reservierung?
- [ ] Ist Docker vorinstalliert — und mit **Compose-Plugin** (`docker compose`, nicht `docker-compose`)?
- [ ] Darf mein Benutzer in die `docker`-Gruppe?
- [ ] **Snapshot vor dem Workshop** möglich? Wer macht ihn?
- [ ] Startet die VM nach einem Neustart des Hosts von selbst?

### Netz

- [ ] Welches Subnetz? (brauche ich für die Firewall-Regel)
- [ ] Port 80 eingehend aus dem Teilnehmer-Subnetz erlaubt?
- [ ] Gibt es einen Zwangs-Proxy für HTTP im Browser? (der würde interne Adressen umleiten)
- [ ] Eigenes VLAN oder Gastnetz für die VM möglich?
- [ ] **Ausdrücklich klären:** keine Freigabe nach außen, keine Portweiterleitung

### Zertifikate und HTTPS

- [ ] Interne CA vorhanden? (AD Certificate Services oder anderes)
- [ ] Sind die Schulungsrechner domänengebunden?
- [ ] Kann ein Wurzelzertifikat per Gruppenrichtlinie verteilt werden?
- [ ] Gibt es eine Firmendomain, unter der ein **öffentliches** Zertifikat möglich wäre?
- [ ] Gibt es eine Vorgabe, dass interne Dienste HTTPS sprechen müssen?

### Zugang und Betrieb

- [ ] SSH auf die VM — von wo aus?
- [ ] Wer darf sie neu starten, wenn ich nicht da bin?
- [ ] Werden VMs zentral gesichert, oder mache ich das selbst?
- [ ] Wie lange darf die VM stehen bleiben — nur für den Workshop oder dauerhaft?
- [ ] Ansprechpartner am Workshoptag, falls etwas klemmt

### Teilnehmerrechner

- [ ] Wie viele Teilnehmer?
- [ ] Firmenrechner oder eigene Geräte?
- [ ] Welcher Browser ist Standard? (**Firefox hat einen eigenen
      Zertifikatsspeicher** und ignoriert den von Windows — relevant, sobald wir
      HTTPS machen)
- [ ] Haben die Teilnehmer lokale Administratorrechte?
- [ ] Ist **DNS über HTTPS** in den Browsern aktiv? (das umgeht den internen
      DNS-Server, dann löst der interne Name nicht auf)

---

## Was welche Antwort auslöst

Damit du beim Gespräch schon einordnen kannst, was du hörst:

| Antwort | Was wir daraufhin bauen |
|---|---|
| Interne CA + Domänenrechner | **HTTPS mit ihrem Zertifikat.** Ich ergänze einen `listen 443`-Block, sie liefern die Datei. Am Backend ändert sich nichts. Schloss im Browser, null Aufwand für die Teilnehmer. |
| Domäne, aber keine CA | HTTPS mit eigener CA, Wurzelzertifikat per GPO. Ergebnis für die Teilnehmer identisch. |
| Weder noch | Bleibt bei http. Läuft heute schon. |
| Kein DNS-Eintrag | Zugriff über die IP. Kein Problem, solange kein Zertifikat im Spiel ist. |
| Kein Internet auf der VM | Ich baue die Images vorher und bringe sie als Datei mit. |
| Client-Isolation im Netz | Muss vorher gelöst werden, sonst nützt der ganze Rest nichts. |

---

## Der Zettel zum Mitnehmen

Wenn du nur eine Karte in der Hand haben willst:

1. **Interne CA? Rechner in der Domäne?** → entscheidet über HTTPS
2. **A-Record auf die VM-IP möglich?** → entscheidet über den Namen
3. **Hat die VM Internet?** → entscheidet, ob ich Images mitbringen muss
4. **Erreichen die Schulungsrechner die VM?** → einmal vorher testen
5. **Snapshot vor dem Workshop?** → weil auf der VM fremder Code läuft
6. **Port 80 aus dem Teilnehmer-Subnetz erlaubt?**
7. **Ansprechpartner am Workshoptag?**

---

## Wenn sie fragen „warum nicht einfach HTTPS?"

Die ehrliche Antwort, die du geben kannst:

> „Weil es für einen internen Namen kein Zertifikat gibt, dem Browser von sich
> aus trauen. Ein selbstsigniertes bringt jedem Teilnehmer bei jedem Besuch eine
> ganzseitige Warnung — das ist schlechter als http, weil es beibringt,
> Sicherheitswarnungen wegzuklicken. Ein Zertifikat aus **eurer** CA hätte das
> Problem nicht, deshalb frage ich danach. Das Tool ist darauf vorbereitet: der
> Wechsel ist Konfiguration, keine Änderung am Programm."
