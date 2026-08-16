// Wie viele unterscheidbare Ausgaenge hat ein API-Aufruf? Fuenf, nicht zwei.
//
// Im stillgelegten Blazor-Frontend war "null" das einzige Fehlersignal. Damit
// fielen "gibt es nicht" und "Server nicht erreichbar" zusammen, und bei
// gestopptem Backend stand auf der Aufgabenseite "Diese Aufgabe gibt es nicht
// (mehr)" — die denkbar irrefuehrendste Auskunft. Dieser Typ macht den
// Unterschied unuebersehbar: wer ihn auswertet, muss alle Faelle behandeln.
//
// "unauthorized" kam mit dem Admin-Bereich dazu, aus demselben Grund: liefe ein
// abgelaufenes Cookie als "rejected" durch, zeigte die Verwaltung eine
// Fehlermeldung statt der Anmeldung, und niemand kaeme je wieder hinein.
export type ApiResult<T> =
  | { kind: 'ok'; value: T }
  | { kind: 'notFound'; message: string }
  | { kind: 'unauthorized'; message: string }
  | { kind: 'rejected'; message: string }
  | { kind: 'unreachable'; message: string }

const DEFAULT_BASE_URL = 'http://localhost:5120'

// Im Betrieb ueber VITE_API_URL gesetzt. Der Standard zeigt auf das lokale
// Backend aus scripts/start-dev.ps1.
export const apiBaseUrl: string = import.meta.env.VITE_API_URL || DEFAULT_BASE_URL

// Das Backend antwortet auf Ablehnungen mit text/plain und fertigen deutschen
// Saetzen ("'notiz.txt' ist keine .java-Datei."). Die werden dem Teilnehmer im
// Wortlaut gezeigt, nicht durch eine eigene Meldung ersetzt.
async function readMessage(response: Response, fallback: string): Promise<string> {
  try {
    const text = (await response.text()).trim()
    return text.length > 0 ? text : fallback
  } catch {
    return fallback
  }
}

type RequestOptions = {
  method?: string
  body?: BodyInit
  signal?: AbortSignal
  headers?: Record<string, string>
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<ApiResult<T>> {
  let response: Response

  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      method: options.method ?? 'GET',
      body: options.body,
      signal: options.signal,
      // text/plain steht bewusst VORNE. Die API gibt Ablehnungen als nackten
      // String zurueck (BadRequest("...")); ASP.NET waehlt dafuer den ersten
      // passenden Formatter aus dem Accept-Kopf. Stand application/json vorn,
      // kam die Meldung JSON-kodiert mit Anfuehrungszeichen an und wurde dem
      // Teilnehmer als »"'notiz.txt' ist keine .java-Datei."« gezeigt.
      // Objekte liefert die API weiterhin als JSON — fuer die kann der
      // StringOutputFormatter nicht einspringen.
      headers: { Accept: 'text/plain, application/json', ...options.headers },
      // Das Anmelde-Cookie des Admin-Bereichs liegt auf der API (Port 5120),
      // die Seite laeuft auf 5173. Ohne "include" laesst der Browser es bei
      // jedem Aufruf weg, und jeder Admin-Endpunkt antwortet mit 401.
      credentials: 'include',
    })
  } catch (error) {
    // Hierher fuehrt nur ein Netzwerkfehler — der Server hat gar nicht
    // geantwortet. Ein abgebrochener Aufruf ist kein Fehler des Servers.
    if (error instanceof DOMException && error.name === 'AbortError') throw error

    return {
      kind: 'unreachable',
      message: 'Der Server ist nicht erreichbar. Läuft das Backend?',
    }
  }

  // 403 kommt hier mit hinein: fuer ein Frontend mit genau einer Rolle ist
  // "darf nicht" dasselbe wie "nicht angemeldet" — in beiden Faellen hilft nur
  // die Anmeldung weiter.
  if (response.status === 401 || response.status === 403) {
    return {
      kind: 'unauthorized',
      message: await readMessage(response, 'Für diesen Bereich ist eine Anmeldung nötig.'),
    }
  }

  if (response.status === 404) {
    return { kind: 'notFound', message: await readMessage(response, 'Nicht gefunden.') }
  }

  if (!response.ok) {
    return {
      kind: 'rejected',
      message: await readMessage(response, `Der Server hat mit ${response.status} geantwortet.`),
    }
  }

  if (response.status === 204) {
    return { kind: 'ok', value: undefined as T }
  }

  try {
    return { kind: 'ok', value: (await response.json()) as T }
  } catch {
    return {
      kind: 'rejected',
      message: 'Die Antwort des Servers war unlesbar.',
    }
  }
}

// Schreibende Aufrufe mit JSON-Rumpf.
//
// Der Content-Type steht hier und nicht in request(): beim Hochladen von
// Dateien muss ihn der Browser selbst bilden, weil er die multipart-Grenze
// enthaelt. Wer ihn dort pauschal setzt, macht jeden Upload kaputt.
export function jsonRequest<T>(
  path: string,
  method: string,
  body?: unknown,
  signal?: AbortSignal,
): Promise<ApiResult<T>> {
  const hasBody = body !== undefined

  return request<T>(path, {
    method,
    body: hasBody ? JSON.stringify(body) : undefined,
    headers: hasBody ? { 'Content-Type': 'application/json' } : undefined,
    signal,
  })
}
