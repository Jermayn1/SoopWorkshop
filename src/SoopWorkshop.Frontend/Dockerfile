# Frontend: Node baut, nginx liefert aus und ist zugleich der Reverse Proxy
# vor dem Backend. Dieser Container ist der EINZIGE, der Ports veroeffentlicht.
#
# Build-Kontext ist dieser Ordner (context: ./src/SoopWorkshop.Frontend).

# ---------------------------------------------------------------- Bauen -------
FROM node:22-alpine AS build
WORKDIR /quelle

# Erst die Paketlisten, damit die Installationsschicht im Cache bleibt, solange
# sich keine Abhaengigkeit aendert. npm ci statt npm install: es haelt sich
# streng an package-lock.json, statt Versionen neu aufzuloesen.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# LEER, und das ist die eigentliche Aussage: das Frontend spricht die API ueber
# denselben Ursprung an (relative Pfade), weil nginx unten /api weiterreicht.
# Damit steckt kein Hostname im Image.
#
# Die leere Zeichenkette muss ankommen - client.ts unterscheidet ausdruecklich
# zwischen "nicht gesetzt" (Entwicklung, localhost:5120) und "leer" (gleicher
# Ursprung).
ENV VITE_API_URL=""

# Enthaelt tsc -b: ein Typfehler gegen den erzeugten API-Vertrag laesst den
# Image-Build fallen, statt erst im Browser aufzufallen.
RUN npm run build

# --------------------------------------------------------------- Laufen -------
FROM nginx:alpine AS final

# openssl fuer das Notfall-Zertifikat im Entrypoint. Das nginx-Image bringt es
# nicht mit.
RUN apk add --no-cache openssl

COPY --from=build /quelle/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
COPY docker-entrypoint.sh /docker-entrypoint-soop.sh
RUN chmod +x /docker-entrypoint-soop.sh

EXPOSE 80 443

ENTRYPOINT ["/docker-entrypoint-soop.sh"]
CMD ["nginx", "-g", "daemon off;"]
