// Wie viele unterscheidbare Ausgaenge hat ein API-Aufruf? Vier, nicht zwei.
//
// Im stillgelegten Blazor-Frontend war "null" das einzige Fehlersignal. Damit
// fielen "gibt es nicht" und "Server nicht erreichbar" zusammen, und bei
// gestopptem Backend stand auf der Aufgabenseite "Diese Aufgabe gibt es nicht
// (mehr)" — die denkbar irrefuehrendste Auskunft. Dieser Typ macht den
// Unterschied unuebersehbar: wer ihn auswertet, muss alle Faelle behandeln.
export type ApiResult<T> =
  | { kind: 'ok'; value: T }
  | { kind: 'notFound'; message: string }
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
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<ApiResult<T>> {
  let response: Response

  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      method: options.method ?? 'GET',
      body: options.body,
      signal: options.signal,
      headers: { Accept: 'application/json, text/plain' },
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
