import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { animate, motion, useMotionValue, useTransform } from 'framer-motion'
import { AlertTriangle, ArrowLeft, Clock, Loader2, Trophy } from 'lucide-react'
import { CategoryCard } from '../components/CategoryCard'
import { useSubmissionPolling } from '../hooks/useSubmissionPolling'
import { fetchSubmissionState } from '../api/endpoints'
import type { EvaluationCategory } from '../api/types'

// Anzeigereihenfolge wie in SoopWorkshop.Shared/Constants/EvaluationCategoryOrder.cs.
// Die API liefert bereits sortiert; das Frontend sortiert trotzdem selbst.
const CATEGORY_ORDER: EvaluationCategory[] = ['CleanCode', 'Compilability', 'Functionality']

function orderOf(category: EvaluationCategory): number {
  const index = CATEGORY_ORDER.indexOf(category)
  return index < 0 ? CATEGORY_ORDER.length : index
}

function ScoreCircle({ score }: { score: number }) {
  const count = useMotionValue(0)
  const rounded = useTransform(count, (value) => Math.round(value))

  useEffect(() => {
    const controls = animate(count, score, { duration: 1.2, delay: 0.3, ease: 'easeOut' })
    return () => controls.stop()
  }, [count, score])

  const great = score >= 80
  // Der hellste Punkt des Verlaufs traegt die weisse Zahl und muss deshalb
  // 3:1 halten (Grossschrift). Nachgemessen: emerald-500 lag bei 2,47:1,
  // emerald-600 liegt bei 3,77:1. Blau (3,76) und Rot (3,75) passen ab -500.
  const gradient = great
    ? 'from-emerald-600 to-emerald-800'
    : score >= 50
      ? 'from-blue-500 to-blue-700'
      : 'from-rose-500 to-rose-700'

  return (
    <motion.div
      initial={{ scale: 0, rotate: -15 }}
      animate={{ scale: 1, rotate: 0 }}
      transition={{ type: 'spring', stiffness: 180, damping: 18 }}
      className="relative inline-block mb-6"
    >
      <div
        className={`w-36 h-36 rounded-full bg-gradient-to-br ${gradient} flex items-center justify-center shadow-2xl ring-8 ring-white`}
      >
        <div className="text-white text-center">
          <motion.div className="text-5xl font-black tabular-nums leading-none">
            {rounded}
          </motion.div>
          <div className="text-xs font-semibold uppercase tracking-widest opacity-90 mt-1">
            von 100
          </div>
        </div>
      </div>
      {great && (
        <motion.div
          animate={{ rotate: [0, 12, -12, 12, 0] }}
          transition={{ repeat: Infinity, duration: 2.5, ease: 'easeInOut' }}
          className="absolute -top-3 -right-3 bg-amber-500 w-11 h-11 rounded-full flex items-center justify-center shadow-lg border-4 border-white"
        >
          <Trophy className="w-5 h-5 text-white" aria-hidden="true" />
        </motion.div>
      )}
    </motion.div>
  )
}

function Waiting({ title, text }: { title: string; text: string }) {
  return (
    <div className="flex-1 flex items-center justify-center bg-slate-50 p-8">
      <motion.div
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        className="max-w-md text-center"
        role="status"
        aria-live="polite"
      >
        <Loader2
          className="w-10 h-10 text-indigo-600 animate-spin mx-auto mb-5"
          aria-hidden="true"
        />
        <h2 className="text-2xl font-bold text-slate-800 mb-2">{title}</h2>
        <p className="text-slate-600">{text}</p>
      </motion.div>
    </div>
  )
}

export function ResultPage() {
  const { submissionId = '' } = useParams()
  const { phase } = useSubmissionPolling(submissionId)

  // Für den Zurück-Link zur richtigen Aufgabe. Das Feld kommt seit
  // Etappe 4.0 auf dem Status mit, damit ein direkt aufgerufener
  // Ergebnis-Link ebenfalls einen Weg zurück hat.
  const [taskId, setTaskId] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    fetchSubmissionState(submissionId, controller.signal)
      .then((result) => {
        if (result.kind === 'ok') setTaskId(result.value.taskItemId)
      })
      .catch(() => {
        /* Der Zurück-Link ist Beiwerk — sein Fehlschlag darf nichts stören. */
      })
    return () => controller.abort()
  }, [submissionId])

  const backLink = (
    <Link
      to={taskId ? `/aufgaben/${taskId}` : '/'}
      className="flex items-center gap-2 text-slate-600 text-sm font-semibold hover:text-indigo-700 transition-colors mb-8 group w-fit"
    >
      <ArrowLeft
        className="w-4 h-4 group-hover:-translate-x-0.5 transition-transform"
        aria-hidden="true"
      />
      {taskId ? 'Zurück zur Aufgabe' : 'Zur Aufgabenliste'}
    </Link>
  )

  // Warteschlange und Prüfung sind verschiedene Zustände und bekommen
  // verschiedene Texte — "wartet" ist etwas anderes als "wird geprüft".
  if (phase.kind === 'idle' || phase.kind === 'pending') {
    return (
      <Waiting
        title="In der Warteschlange"
        text="Deine Abgabe ist angekommen und wartet auf einen freien Platz."
      />
    )
  }

  if (phase.kind === 'running') {
    return (
      <Waiting
        title="Wird gerade geprüft"
        text="Kompilieren, Testfälle, Unit-Tests. Das dauert meist ein paar Sekunden."
      />
    )
  }

  if (phase.kind === 'failed') {
    return (
      <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
        <div className="max-w-3xl mx-auto">
          {backLink}
          <motion.div
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            className="rounded-2xl border border-rose-200 bg-white overflow-hidden shadow-sm"
          >
            <div className="flex items-center gap-2 px-5 py-4 border-b border-rose-100 bg-rose-50">
              <AlertTriangle className="w-5 h-5 text-rose-700" aria-hidden="true" />
              <h2 className="font-bold text-slate-800">Die Auswertung ist nicht durchgelaufen</h2>
            </div>
            <div className="p-5 space-y-4">
              {/* Der Grund des Servers im Wortlaut — kein stiller Fehlschlag. */}
              <p className="whitespace-pre-wrap text-slate-700">{phase.message}</p>
              <div className="flex items-center gap-2 text-sm text-slate-600">
                <Clock className="w-4 h-4" aria-hidden="true" />
                Du kannst die Aufgabe erneut abgeben.
              </div>
            </div>
          </motion.div>
        </div>
      </div>
    )
  }

  const { result } = phase
  const categories = [...result.categoryResults].sort(
    (a, b) => orderOf(a.category) - orderOf(b.category),
  )
  const allTests = categories.flatMap((c) => c.testCaseResults)
  const passed = allTests.filter((t) => t.passed).length
  const open = allTests.length - passed

  const headline =
    result.totalScore >= 80
      ? 'Hervorragende Arbeit!'
      : result.totalScore >= 50
        ? 'Guter Versuch!'
        : 'Da geht noch mehr!'

  return (
    <div className="flex-1 overflow-y-auto bg-slate-50">
      <div className="max-w-3xl mx-auto px-6 py-10 pb-24">
        {backLink}

        <div className="text-center mb-10">
          <ScoreCircle score={result.totalScore} />
          <motion.h1
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.4 }}
            className="text-3xl font-extrabold text-slate-900 mb-2"
          >
            {headline}
          </motion.h1>
          <motion.p
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.5 }}
            className="text-slate-600 text-sm"
          >
            {open === 0
              ? 'Alle Teilprüfungen bestanden.'
              : `${passed} bestanden, ${open} offen. Klapp die Kategorien auf, um zu sehen, woran es liegt.`}
          </motion.p>
        </div>

        <div className="space-y-3">
          {categories.map((category, index) => (
            <CategoryCard key={category.id} result={category} delay={0.1 + index * 0.1} />
          ))}
        </div>
      </div>
    </div>
  )
}
