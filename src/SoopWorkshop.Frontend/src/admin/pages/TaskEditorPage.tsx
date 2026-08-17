import { Link, useParams } from 'react-router-dom'
import { AlertCircle, ArrowLeft } from 'lucide-react'
import { useAdminCatalog } from '../adminOutlet'
import { DIFFICULTY_LABELS, MODE_LABELS } from '../../api/labels'

// Noch kein Editor, aber auch kein Platzhalter ins Blaue: die Grunddaten
// stehen bereits im geladenen Bestand, also werden sie gezeigt. Die Masken
// zum Bearbeiten kommen in Etappe 5.2.
export function TaskEditorPage() {
  const { taskId = '' } = useParams()
  const { categories, loading } = useAdminCatalog()

  const category = categories.find((c) => c.tasks.some((t) => t.id === taskId))
  const task = category?.tasks.find((t) => t.id === taskId)

  if (loading) {
    return (
      <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
        <div className="mx-auto w-full max-w-3xl space-y-4" aria-hidden="true">
          <div className="h-10 w-2/3 rounded-xl bg-slate-200 animate-pulse" />
          <div className="h-40 rounded-2xl bg-slate-200 animate-pulse" />
        </div>
      </div>
    )
  }

  if (!task || !category) {
    return (
      <div className="flex flex-1 items-center justify-center bg-slate-50 p-8">
        <div className="max-w-md rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
          <div className="mx-auto mb-4 flex w-16 h-16 items-center justify-center rounded-3xl bg-slate-100">
            <AlertCircle className="w-8 h-8 text-slate-500" aria-hidden="true" />
          </div>
          <h2 className="mb-2 text-2xl font-bold text-slate-800">Diese Aufgabe gibt es nicht</h2>
          <p className="text-slate-600">
            Sie steht nicht im geladenen Bestand. Vielleicht wurde sie gelöscht.
          </p>
          <Link
            to="/admin"
            className="mt-6 inline-flex items-center gap-1.5 text-sm font-semibold text-slate-700 hover:underline"
          >
            <ArrowLeft className="w-4 h-4" aria-hidden="true" />
            Zurück zur Übersicht
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
      <div className="mx-auto w-full max-w-3xl anim-auf">
        <Link
          to="/admin"
          className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-600 hover:text-slate-900 hover:underline"
        >
          <ArrowLeft className="w-4 h-4" aria-hidden="true" />
          Übersicht
        </Link>

        <h1 className="mt-3 text-2xl font-bold text-slate-800">{task.title}</h1>

        <dl className="mt-6 grid grid-cols-1 gap-px overflow-hidden rounded-2xl border border-slate-200 bg-slate-200 sm:grid-cols-2">
          {[
            ['Kategorie', category.name],
            ['Reihenfolge', String(task.order)],
            ['Schwierigkeit', DIFFICULTY_LABELS[task.difficulty]],
            ['Auswertung', MODE_LABELS[task.evaluationMode]],
            ['Sichtbarkeit', task.isVisible ? 'Für Teilnehmer sichtbar' : 'Verborgen'],
          ].map(([label, value]) => (
            <div key={label} className="bg-white px-5 py-3">
              <dt className="text-xs font-semibold uppercase tracking-wider text-slate-500">
                {label}
              </dt>
              <dd className="mt-0.5 text-slate-800">{value}</dd>
            </div>
          ))}
        </dl>

        <p className="mt-6 rounded-xl border border-slate-200 bg-white p-4 text-sm text-slate-600">
          Bearbeiten, Testfälle, JUnit-Dateien und Gewichte folgen in Etappe 5.2.
        </p>
      </div>
    </div>
  )
}
