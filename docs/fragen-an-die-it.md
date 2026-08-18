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

## Ausgangslage

Bekannt und geklärt:

- **Die Teilnehmer bringen eigene Geräte mit** und hängen im **Gast-WLAN**
- Die VM ist aus dem Gastnetz erreichbar
- Die VM hat Internet

Daraus folgt zweierlei, bevor du überhaupt fragst:

**Eine interne CA nützt hier nichts.** Sie funktioniert nur auf Geräten, die der
CA vertrauen — also auf verwalteten Firmenrechnern. Auf privaten Handys und
Laptops müsste jeder Teilnehmer selbst ein Wurzelzertifikat installieren. Das
verlangt man nicht, und die meisten könnten es auch gar nicht.

**Ein interner DNS-Eintrag nützt im Gastnetz vermutlich nichts.** Gastnetze
bekommen meist den Router oder einen öffentlichen Resolver als DNS, nicht den
internen Server. Frag trotzdem — aber rechne mit der IP.

**Der Aufbau läuft heute schon über die IP und braucht nichts davon.** Die
Fragen unten dienen dazu, es *schöner* zu machen, nicht *lauffähig*.

---

## Die drei Fragen, die etwas ändern

### 1. Erreichen Geräte im Gast-WLAN die VM wirklich? *(einmal ausprobieren)*

> „Ich brauche Port 80 von einem Gerät im Gast-WLAN auf die VM. Gibt es
> Client-Isolation oder eine Firewall dazwischen?"

Du sagst, es geht — dann **einmal mit dem eigenen Handy im Gast-WLAN
gegenprüfen**, sobald die VM steht. Das ist der einzige Test, der zählt.

Häufige Stolpersteine: Client-Isolation im WLAN, ein Zwangs-Proxy im Browser,
oder eine Firewall zwischen Gast-VLAN und Server-VLAN.

**Wenn es nicht geht,** ist die naheliegende Lösung, die VM **ins Gastnetz zu
stellen**. Das ist sogar aus Sicherheitssicht die bessere Wahl: auf ihr läuft
fremder Java-Code, und dort ist sie vom Firmennetz getrennt.

### 2. Gibt es eine Domain, die dem Betrieb gehört?

**Das ist die einzige Frage, die zu einem Schloss im Browser führt.**

> „Habt ihr eine Domain, unter der ich einen Namen bekommen könnte — mit der
> Möglichkeit, ein öffentliches Zertifikat über eine DNS-Challenge zu holen?"

| Antwort | Was das bedeutet |
|---|---|
| **Ja, mit DNS-API-Zugang** | Ein echtes Let's-Encrypt-Zertifikat. **Jedes Gerät zeigt ein Schloss, ohne dass ein Teilnehmer etwas tut** — auch private Handys. Einmalig ein bis zwei Stunden, danach wartungsfrei. |
| Ja, aber DNS nur von Hand | Geht auch, die Erneuerung wird dann alle 90 Tage Handarbeit |
| Nein | Bleibt bei http. Oder ich registriere selbst eine Domain (~20–30 €/Jahr) |

**Falls ja, nachfragen:** Wer verwaltet die DNS-Einträge? Ist ein Eintrag
möglich, der auf eine **private IP** zeigt? (Das ist erlaubt und üblich, wird
aber manchmal abgelehnt.)

### 3. Interner DNS-Eintrag — gilt der auch im Gastnetz?

> „Kann ich einen A-Record auf die IP der VM bekommen? Und bekommen Geräte im
> Gast-WLAN euren internen DNS überhaupt zu sehen?"

Die zweite Hälfte ist die entscheidende. Ein Eintrag, den das Gastnetz nicht
auflöst, hilft niemandem.

| Antwort | Was das bedeutet |
|---|---|
| Ja, und Gastnetz sieht ihn | Teilnehmer tippen einen Namen statt einer IP |
| Eintrag ja, Gastnetz nein | Nutzlos für die Teilnehmer |
| Nein | Zugriff über die IP — funktioniert |

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

Eine interne CA hilft hier nicht — die Teilnehmer bringen eigene Geräte mit.
Deshalb geht es nur um den öffentlichen Weg:

- [ ] Gibt es eine Domain, die dem Betrieb gehört?
- [ ] Wer verwaltet deren DNS-Einträge? Gibt es dort eine API?
      (nötig für die automatische Erneuerung per DNS-Challenge)
- [ ] Darf ein Eintrag auf eine **private IP** zeigen?
- [ ] Gibt es eine Vorgabe, dass interne Dienste HTTPS sprechen müssen?
      (dann wird aus dem „schöner" ein „muss")

### Zugang und Betrieb

- [ ] SSH auf die VM — von wo aus?
- [ ] Wer darf sie neu starten, wenn ich nicht da bin?
- [ ] Werden VMs zentral gesichert, oder mache ich das selbst?
- [ ] Wie lange darf die VM stehen bleiben — nur für den Workshop oder dauerhaft?
- [ ] Ansprechpartner am Workshoptag, falls etwas klemmt

### Teilnehmer

- [ ] Wie viele? (bestimmt `Evaluation__MaxConcurrency` und die VM-Größe)
- [ ] Läuft das Gast-WLAN über einen Zwangs-Proxy?
- [ ] Bekommt das Gast-WLAN einen öffentlichen DNS-Resolver? (dann greift ein
      interner Eintrag dort nicht)

---

## Was welche Antwort auslöst

Damit du beim Gespräch schon einordnen kannst, was du hörst:

| Antwort | Was wir daraufhin bauen |
|---|---|
| **Domain vorhanden, DNS mit API** | **Der Volltreffer.** Öffentliches Let's-Encrypt-Zertifikat über die DNS-Challenge, A-Record auf die interne IP. Schloss auf jedem Gerät, auch privaten Handys, ohne dass ein Teilnehmer etwas tut. Ein bis zwei Stunden einmalig. |
| Domain vorhanden, DNS nur von Hand | Dasselbe, aber die Erneuerung alle 90 Tage ist Handarbeit. Für einen Workshop trotzdem in Ordnung. |
| Keine Domain | Bleibt bei http — läuft heute schon. Oder ich registriere selbst eine (~20–30 €/Jahr). |
| Interner A-Record, im Gastnetz sichtbar | Teilnehmer tippen einen Namen statt einer IP. Am Server nichts zu ändern. |
| Interner A-Record, im Gastnetz unsichtbar | Nutzlos für die Teilnehmer — dann die IP. |
| Gastnetz erreicht die VM nicht | **Blocker.** Muss vorher gelöst werden, am besten indem die VM ins Gastnetz wandert. |

---

## Der Zettel zum Mitnehmen

1. **Port 80 aus dem Gast-WLAN auf die VM offen?** → mit dem Handy gegenprüfen
2. **Gibt es eine Domain des Betriebs, unter der ich einen Namen bekomme?**
   → der einzige Weg zum Schloss im Browser
3. **A-Record möglich — und sieht das Gastnetz den internen DNS?**
4. **Snapshot vor dem Workshop?** → weil auf der VM fremder Code läuft
5. **Darf die VM ins Gastnetz statt ins Servernetz?** → besser für beide Seiten
6. **Ansprechpartner am Workshoptag?**

---

## Wenn sie fragen „warum nicht einfach HTTPS?"

Die ehrliche Antwort, die du geben kannst:

> „Weil die Teilnehmer eigene Geräte mitbringen. Ein Zertifikat aus eurer CA
> würde auf privaten Handys nicht anerkannt, und ein selbstsigniertes bringt
> jedem bei jedem Besuch eine ganzseitige Warnung — das ist schlechter als
> http, weil es beibringt, Sicherheitswarnungen wegzuklicken. Sauber ginge es
> nur mit einem öffentlichen Zertifikat auf einer Domain, die euch gehört —
> deshalb frage ich danach. Das Tool ist darauf vorbereitet: der Wechsel ist
> Konfiguration, keine Änderung am Programm."
