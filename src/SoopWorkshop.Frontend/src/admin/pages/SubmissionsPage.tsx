import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { AlertTriangle, ChevronLeft, ChevronRight, Loader2, RefreshCw } from 'lucide-react'
import { useAdminCatalog } from '../adminOutlet'
import { fetchSubmissions, type SubmissionListItem, type SubmissionPage } from '../api/submissions'
import type { SubmissionStatus } from '../../api/types'

const SEITENGROESSE = 25

// Dieselben vier Zustaende wie beim Teilnehmer, nur kompakter. "Pending" und
// "Running" bleiben getrennt: in der Warteschlange stehen ist etwas anderes
// als geprueft werden, und beim Nachsehen waehrend des Workshops ist genau das
// der Unterschied, der interessiert.
const STATUS_TEXT: Record<SubmissionStatus, string> = {
  Pending: 'In der Warteschlange',
  Running: 'Wird geprüft',
  Done: 'Fertig',
  Failed: 'Fehlgeschlagen',
}

// Akzentfarben nie als Schriftfarbe auf heller Flaeche — dunkler Text auf
// getoentem Grund mit Kante (§6.1).
const STATUS_STIL: Record<SubmissionStatus, string> = {
  Pending: 'bg-slate-50 text-slate-700 border-slate-200',
  Running: 'bg-amber-50 text-amber-900 border-amber-200',
  Done: 'bg-emerald-50 text-emerald-900 border-emerald-200',
  Failed: 'bg-rose-50 text-rose-900 border-rose-200',
}

function Statusmarke({ status }: { status: SubmissionStatus }) {
  return (
    <span
      className={`inline-block rounded-full border px-2.5 py-0.5 text-xs font-medium ${STATUS_STIL[status]}`}
    >
      {STATUS_TEXT[status]}
    </span>
  )
}

function zeitpunkt(iso: string): string {
  if (!iso) return '—'

  const datum = new Date(iso)
  if (Number.isNaN(datum.getTime())) return '—'

  return datum.toLocaleString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function Punkte({ eintrag }: { eintrag: SubmissionListItem }) {
  // Null ist nicht 0. Ein Strich sagt "noch nicht bewertet", eine 0 wuerde
  // behaupten, die Loesung habe nichts erreicht.
  if (eintrag.totalScore === null) {
    return <span className="text-slate-400">—</span>
  }

  return (
    <span className="font-medium tabular-nums text-slate-800">
      {eintrag.totalScore}
      <span className="text-slate-400"> / {eintrag.maxScore ?? 100}</span>
    </span>
  )
}

export function SubmissionsPage() {
  const { categories } = useAdminCatalog()

  const [seite, setSeite] = useState<SubmissionPage | null>(null)
  const [laedt, setLaedt] = useState(true)
  const [fehler, setFehler] = useState<string | null>(null)

  const [skip, setSkip] = useState(0)
  const [status, setStatus] = useState<SubmissionStatus | ''>('')
  const [aufgabe, setAufgabe] = useState('')

  const laden = useCallback(
    async (signal?: AbortSignal) => {
      setLaedt(true)
      setFehler(null)

      const result = await fetchSubmissions(
        {
          skip,
          take: SEITENGROESSE,
          status: status === '' ? undefined : status,
          taskItemId: aufgabe === '' ? undefined : aufgabe,
        },
        signal,
      )

      if (signal?.aborted) return

      // Alle Ausgaenge behandeln, nicht nur ok und "sonst". Ein nicht
      // erreichbarer Server darf hier nicht als "keine Abgaben" erscheinen.
      if (result.kind === 'ok') {
        setSeite(result.value)
      } else {
        setFehler(result.message)
        setSeite(null)
      }

      setLaedt(false)
    },
    [skip, status, aufgabe],
  )

  useEffect(() => {
    const controller = new AbortController()
    void laden(controller.signal)
    return () => controller.abort()
  }, [laden])

  const alleAufgaben = categories.flatMap((kategorie) =>
    kategorie.tasks.map((task) => ({ id: task.id, label: `${kategorie.name} — ${task.title}` })),
  )

  const gesamt = seite?.total ?? 0
  const bisher = (seite?.skip ?? 0) + (seite?.items.length ?? 0)
  const hatVorige = skip > 0
  const hatWeitere = bisher < gesamt

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-800">Abgaben</h1>
          <p className="text-sm text-slate-500">
            Was die Teilnehmer eingereicht haben, neueste zuerst.
          </p>
        </div>

        <button
          type="button"
          onClick={() => void laden()}
          className="inline-flex items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
        >
          <RefreshCw className="h-4 w-4" aria-hidden="true" />
          Neu laden
        </button>
      </header>

      <div className="flex flex-wrap gap-3">
        <label className="text-sm">
          <span className="mb-1 block font-medium text-slate-600">Status</span>
          <select
            value={status}
            onChange={(event) => {
              setStatus(event.target.value as SubmissionStatus | '')
              // Zurueck auf Seite 1: sonst zeigt ein Filter mit weniger
              // Treffern eine leere Seite 3 und sieht nach "nichts da" aus.
              setSkip(0)
            }}
            className="rounded-lg border border-slate-300 bg-white px-3 py-2"
          >
            <option value="">Alle</option>
            {(Object.keys(STATUS_TEXT) as SubmissionStatus[]).map((wert) => (
              <option key={wert} value={wert}>
                {STATUS_TEXT[wert]}
              </option>
            ))}
          </select>
        </label>

        <label className="text-sm">
          <span className="mb-1 block font-medium text-slate-600">Aufgabe</span>
          <select
            value={aufgabe}
            onChange={(event) => {
              setAufgabe(event.target.value)
              setSkip(0)
            }}
            className="max-w-xs rounded-lg border border-slate-300 bg-white px-3 py-2"
          >
            <option value="">Alle</option>
            {alleAufgaben.map((eintrag) => (
              <option key={eintrag.id} value={eintrag.id}>
                {eintrag.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      {fehler && (
        <div className="flex items-start gap-3 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-rose-900">
          <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0" aria-hidden="true" />
          <div>
            <p className="font-medium">Die Abgaben konnten nicht geladen werden.</p>
            <p className="text-sm">{fehler}</p>
          </div>
        </div>
      )}

      {laedt && (
        <div className="flex items-center gap-2 text-slate-500">
          <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
          Wird geladen …
        </div>
      )}

      {!laedt && !fehler && seite?.items.length === 0 && (
        <p className="rounded-xl border border-slate-200 bg-white px-4 py-8 text-center text-slate-500">
          {status === '' && aufgabe === ''
            ? 'Es wurde noch nichts abgegeben.'
            : 'Zu diesem Filter gibt es keine Abgaben.'}
        </p>
      )}

      {!laedt && !fehler && seite && seite.items.length > 0 && (
        <>
          <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-slate-200 bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                <tr>
                  <th className="px-4 py-3">Abgegeben</th>
                  <th className="px-4 py-3">Aufgabe</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Punkte</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {seite.items.map((eintrag) => (
                  <tr key={eintrag.id} className="border-b border-slate-100 last:border-0">
                    <td className="whitespace-nowrap px-4 py-3 tabular-nums text-slate-600">
                      {zeitpunkt(eintrag.submittedAt)}
                    </td>
                    <td className="px-4 py-3">
                      <div className="font-medium text-slate-800">{eintrag.taskTitle}</div>
                      <div className="text-xs text-slate-500">{eintrag.categoryName}</div>
                    </td>
                    <td className="px-4 py-3">
                      <Statusmarke status={eintrag.status} />
                      {eintrag.status === 'Failed' && eintrag.errorMessage && (
                        <p className="mt-1 max-w-md text-xs text-slate-500">
                          {eintrag.errorMessage}
                        </p>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <Punkte eintrag={eintrag} />
                    </td>
                    <td className="px-4 py-3 text-right">
                      {/*
                        Auf DIESELBE Ergebnisseite, die der Teilnehmer sieht.
                        Eine zweite, nachgebaute Anzeige liefe beim ersten Umbau
                        auseinander — dieselbe Entscheidung wie bei der Vorschau
                        in Etappe 5.5.
                      */}
                      <Link
                        to={`/abgaben/${eintrag.id}`}
                        className="rounded-lg px-3 py-1.5 font-medium text-indigo-700 hover:bg-indigo-50"
                      >
                        Ergebnis
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between text-sm text-slate-600">
            <span className="tabular-nums">
              {seite.skip + 1}–{bisher} von {gesamt}
            </span>

            <div className="flex gap-2">
              <button
                type="button"
                disabled={!hatVorige}
                onClick={() => setSkip(Math.max(0, skip - SEITENGROESSE))}
                className="inline-flex items-center gap-1 rounded-lg border border-slate-300 bg-white px-3 py-1.5 font-medium disabled:cursor-not-allowed disabled:opacity-50"
              >
                <ChevronLeft className="h-4 w-4" aria-hidden="true" />
                Zurück
              </button>
              <button
                type="button"
                disabled={!hatWeitere}
                onClick={() => setSkip(skip + SEITENGROESSE)}
                className="inline-flex items-center gap-1 rounded-lg border border-slate-300 bg-white px-3 py-1.5 font-medium disabled:cursor-not-allowed disabled:opacity-50"
              >
                Weiter
                <ChevronRight className="h-4 w-4" aria-hidden="true" />
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
