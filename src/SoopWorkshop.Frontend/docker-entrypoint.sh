#!/bin/sh
# Sorgt dafuer, dass ein Zertifikat da ist, bevor nginx startet.
#
# Der vorgesehene Weg ist ein Zertifikat aus der eigenen CA (mkcert), gemountet
# nach /etc/nginx/certs - siehe docs/server-aufsetzen.md. Nur dann bleibt der
# Browser der Teilnehmer ruhig.
#
# Fehlt es, wird ein selbstsigniertes erzeugt, statt den Start abzubrechen:
# "docker compose up" soll auf einem frischen Rechner ein benutzbares System
# ergeben. Der Preis ist die Zertifikatswarnung im Browser, und darauf weist
# dieser Start laut hin - eine stille Notloesung waere schlimmer als keine.
set -e

GEMOUNTET_ZERT=/etc/nginx/certs/soop.pem
GEMOUNTET_SCHLUESSEL=/etc/nginx/certs/soop-key.pem

# Das gemountete Verzeichnis ist absichtlich schreibgeschuetzt (:ro in
# docker-compose.yml) - der Container soll das Zertifikat nicht aendern koennen.
# Das Notfall-Zertifikat kann deshalb NICHT dorthin, es braucht einen eigenen,
# beschreibbaren Ort.
NOTFALL_VERZEICHNIS=/tmp/soop-zertifikat
NOTFALL_ZERT=$NOTFALL_VERZEICHNIS/soop.pem
NOTFALL_SCHLUESSEL=$NOTFALL_VERZEICHNIS/soop-key.pem

# Die Pfade werden bei JEDEM Start neu gesetzt, nicht nur im Notfallzweig.
#
# Grund: ein "docker compose restart" behaelt denselben Container samt
# Dateisystem. Wer zuerst ohne Zertifikat startet und es spaeter nachlegt -
# genau der Weg, den docs/server-aufsetzen.md Abschnitt 11 vorschlaegt - haette
# sonst eine nginx.conf, die weiter auf das alte selbstsignierte in /tmp zeigt.
# Das Log meldete "Zertifikat gefunden", der Browser zeigte weiter eine Warnung,
# und die Fehlersuche liefe in die falsche Richtung.
setze_pfade() {
    sed -i \
        -e "s|ssl_certificate     .*|ssl_certificate     $1;|" \
        -e "s|ssl_certificate_key .*|ssl_certificate_key $2;|" \
        /etc/nginx/nginx.conf
}

if [ -f "$GEMOUNTET_ZERT" ] && [ -f "$GEMOUNTET_SCHLUESSEL" ]; then
    echo "Zertifikat gefunden: $GEMOUNTET_ZERT"
    setze_pfade "$GEMOUNTET_ZERT" "$GEMOUNTET_SCHLUESSEL"
else
    echo "============================================================"
    echo " ACHTUNG: kein Zertifikat unter /etc/nginx/certs gefunden."
    echo ""
    echo " Es wird ein SELBSTSIGNIERTES erzeugt. Die Seite ist damit"
    echo " erreichbar, aber jeder Browser zeigt eine Warnung - auf"
    echo " jedem Rechner, bei jedem Besuch."
    echo ""
    echo " Fuer den Workshop-Betrieb ein Zertifikat aus der eigenen CA"
    echo " erzeugen und nach ./certs legen:"
    echo "   docs/server-aufsetzen.md, Abschnitt 6"
    echo "============================================================"

    mkdir -p "$NOTFALL_VERZEICHNIS"

    # Ohne 2>/dev/null. Schlaegt das Erzeugen fehl, soll der Grund im Protokoll
    # stehen und nicht als endlose Neustartschleife ohne Erklaerung enden -
    # genau so ist diese Datei beim ersten Anlauf gescheitert.
    openssl req -x509 -nodes -newkey rsa:2048 \
        -days 365 \
        -keyout "$NOTFALL_SCHLUESSEL" \
        -out "$NOTFALL_ZERT" \
        -subj "/CN=soop.workshop" \
        -addext "subjectAltName=DNS:soop.workshop,DNS:localhost,IP:127.0.0.1"

    # nginx.conf beschreibt den Normalfall (/etc/nginx/certs) und bleibt damit
    # lesbar; fuer den Notfallpfad werden die beiden Zeilen umgebogen.
    setze_pfade "$NOTFALL_ZERT" "$NOTFALL_SCHLUESSEL"
fi

exec "$@"
