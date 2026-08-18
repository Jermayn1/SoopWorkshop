#!/usr/bin/env bash
# Prueft den laufenden Betrieb auf der VM und sagt, WAS klemmt.
#
# Gedacht fuer den Moment, in dem die Seite "502 Bad Gateway" zeigt oder gar
# nicht antwortet: statt sich durch Logs zu graben, einmal hier durchlaufen
# lassen. Jede Pruefung nennt bei einem Fehlschlag den naechsten Schritt.
#
#   ./scripts/pruefe-betrieb.sh
#
# Nichts davon aendert etwas - das Skript liest nur.

set -u

COMPOSE="docker compose -f docker-compose.yml"

rot=$'\033[31m'; gruen=$'\033[32m'; gelb=$'\033[33m'; grau=$'\033[90m'; klar=$'\033[0m'
fehler=0
warnungen=0

ok()      { printf "  ${gruen}OK  ${klar}  %s\n" "$1"; }
fehl()    { printf "  ${rot}FEHL${klar}  %s\n" "$1"; fehler=$((fehler+1)); }
warnung() { printf "  ${gelb}HINW${klar}  %s\n" "$1"; warnungen=$((warnungen+1)); }
tipp()    { printf "        ${grau}%s${klar}\n" "$1"; }
titel()   { printf "\n%s\n" "$1"; }

cd "$(dirname "$0")/.." || exit 1

printf "\nSoopWorkshop — Betriebspruefung\n"

# ------------------------------------------------------------------ Docker ---
titel "Docker"

if ! command -v docker >/dev/null 2>&1; then
    fehl "docker ist nicht installiert."
    tipp "docs/betrieb.md, Abschnitt 'Docker nachinstallieren'"
    exit 1
fi

if ! docker compose version >/dev/null 2>&1; then
    fehl "Das Compose-Plugin fehlt ('docker compose' funktioniert nicht)."
    tipp "docs/betrieb.md, Abschnitt 'Docker nachinstallieren'"
    exit 1
fi
ok "docker und das Compose-Plugin sind da"

if ! docker info >/dev/null 2>&1; then
    fehl "Der Docker-Daemon antwortet nicht (oder es fehlen die Rechte)."
    tipp "sudo usermod -aG docker \$USER  — danach ab- und wieder anmelden"
    exit 1
fi
ok "der Docker-Daemon antwortet"

# --------------------------------------------------------------------- .env ---
titel ".env"

if [ ! -f .env ]; then
    fehl "Es gibt keine .env."
    tipp "cp .env.example .env  — dann die beiden Passwoerter setzen"
    exit 1
fi
ok ".env ist vorhanden"

# Windows-Zeilenenden. Der Wagenruecklauf wandert in den Wert hinein, und dann
# stimmt ein Passwort nicht, obwohl es richtig aussieht.
if grep -q $'\r' .env 2>/dev/null; then
    fehl ".env hat Windows-Zeilenenden (CRLF)."
    tipp "Der Wagenruecklauf landet IM Passwort. Beheben mit:"
    tipp "sed -i 's/\\r\$//' .env  — danach: $COMPOSE up -d"
else
    ok ".env hat Unix-Zeilenenden"
fi

if grep -q 'bitte-aendern' .env 2>/dev/null; then
    fehl "In der .env steht noch mindestens ein Vorgabewert (bitte-aendern)."
    tipp "POSTGRES_PASSWORD und Admin__Password setzen"
else
    ok "die Vorgabewerte sind ersetzt"
fi

if ! grep -qE '^Admin__Password=.+' .env 2>/dev/null; then
    fehl "Admin__Password fehlt oder ist leer — das Backend startet damit nicht."
else
    ok "Admin__Password ist gesetzt"
fi

# ---------------------------------------------------------------- Container ---
titel "Container"

zustand() { $COMPOSE ps --format '{{.Service}} {{.State}} {{.Health}}' 2>/dev/null | awk -v d="$1" '$1==d {print $2" "$3}'; }

if ! $COMPOSE ps >/dev/null 2>&1; then
    fehl "Compose kann den Stapel nicht lesen — laeuft er ueberhaupt?"
    tipp "$COMPOSE up -d --build"
    exit 1
fi

backend_laeuft=0

for dienst in db backend frontend; do
    z=$(zustand "$dienst")
    if [ -z "$z" ]; then
        fehl "$dienst laeuft nicht."
        tipp "$COMPOSE up -d"
        continue
    fi

    [ "$dienst" = "backend" ] && case "$z" in running*) backend_laeuft=1 ;; esac

    case "$z" in
        "running healthy"|"running ")  ok "$dienst: $z" ;;
        running*unhealthy)             fehl "$dienst: $z" ;;
        restarting*)                   fehl "$dienst startet in einer Schleife neu — es faellt beim Hochfahren." ;;
        exited*)                       fehl "$dienst ist beendet." ;;
        *)                             warnung "$dienst: $z" ;;
    esac
done

# Speicher: der haeufigste Grund, warum das Backend nach einer Abgabe faellt.
if docker inspect --format '{{.State.OOMKilled}}' "$($COMPOSE ps -q backend 2>/dev/null)" 2>/dev/null | grep -q true; then
    fehl "Das Backend wurde wegen Speichermangel abgeraeumt (OOMKilled)."
    tipp "BACKEND_MEMORY in der .env anheben, z. B. BACKEND_MEMORY=3g"
fi

# ------------------------------------------------------------------ Zugriff ---
titel "Erreichbarkeit"

port=$(grep -E '^HTTP_PORT=' .env 2>/dev/null | cut -d= -f2 | tr -d '\r ' )
port=${port:-80}

pruefe_url() {
    code=$(curl -s -o /dev/null -m 8 -w '%{http_code}' "$2" 2>/dev/null)
    case "$code" in
        200) ok "$1 — 200" ;;
        502) fehl "$1 — 502: der Proxy steht, das Backend antwortet ihm nicht."
             tipp "Die Backend-Logs unten sagen warum." ;;
        000) fehl "$1 — keine Antwort. Laeuft das Frontend? Ist der Port belegt?" ;;
        *)   warnung "$1 — $code" ;;
    esac
}

pruefe_url "Seite            " "http://localhost:$port/"
pruefe_url "API              " "http://localhost:$port/api/categories"
pruefe_url "Backend-Health   " "http://localhost:$port/health"

# --------------------------------------------------------------------- Java ---
titel "Auswertung"

# Nur pruefen, wenn der Container ueberhaupt laeuft. Sonst meldete diese
# Pruefung "javac fehlt" fuer ein Image, das voellig in Ordnung ist - eine
# Falschdiagnose, die in die falsche Richtung schickt.
if [ "$backend_laeuft" -eq 0 ]; then
    printf "  ${grau}----  uebersprungen, das Backend laeuft nicht${klar}\n"
elif $COMPOSE exec -T backend javac -version >/dev/null 2>&1; then
    ok "javac ist im Backend-Container vorhanden"
else
    fehl "javac fehlt im Backend-Container — jede Abgabe wuerde scheitern."
    tipp "$COMPOSE build --no-cache backend"
fi

# ------------------------------------------------------------------- Fehler ---
if [ "$fehler" -gt 0 ]; then
    # Erst sammeln, dann entscheiden: eine Ueberschrift ohne Inhalt sieht aus
    # wie ein Fehler des Skripts und schickt in die falsche Richtung.
    auszug=$($COMPOSE logs --tail=200 backend 2>/dev/null \
        | grep -iE 'error|fail|exception|refused|denied|28P01|unhealthy' \
        | grep -viE 'Failed executing DbCommand|Applying migration|ausstehende Migration|EntityFrameworkCore.Database.Command' \
        | tail -15)

    if [ -n "$auszug" ]; then
        titel "Auszug aus dem Backend-Protokoll"
        printf "${grau}  EF Core meldet beim ERSTEN Migrieren planmaessig fehlgeschlagene\n"
        printf "  Abfragen auf noch nicht existierende Tabellen — die sind hier\n"
        printf "  herausgefiltert.${klar}\n\n"
        printf "%s\n" "$auszug" | sed 's/^/  /'
    else
        titel "Backend-Protokoll"
        printf "  ${grau}Keine Fehlermeldungen gefunden. Wenn das Backend nicht laeuft,\n"
        printf "  ist es vermutlich gestoppt und nicht abgestuerzt: $COMPOSE up -d${klar}\n"
    fi
fi

# --------------------------------------------------------------- Ergebnis ----
printf "\n────────────────────────────────────────────────────────\n"

if [ "$fehler" -eq 0 ]; then
    ip=$(hostname -I 2>/dev/null | awk '{print $1}')
    adresse="http://${ip:-<ip-der-vm>}"
    [ "$port" != "80" ] && adresse="$adresse:$port"

    printf "${gruen}Alles in Ordnung.${klar}\n\n"
    printf "  Teilnehmer:  %s\n" "$adresse"
    printf "  Verwaltung:  %s/admin\n\n" "$adresse"
    printf "${grau}  Ob die Teilnehmergeraete diese Adresse auch erreichen, kann nur\n"
    printf "  ein Test VON EINEM SOLCHEN GERAET zeigen — im Gastnetz ist das die\n"
    printf "  haeufigste offene Frage.${klar}\n"
    exit 0
fi

printf "${rot}%s Pruefung(en) gefallen.${klar}" "$fehler"
[ "$warnungen" -gt 0 ] && printf " %s Hinweis(e)." "$warnungen"
printf "\n\nVollstaendige Logs:\n  %s logs --tail=100 backend\n" "$COMPOSE"
exit 1
