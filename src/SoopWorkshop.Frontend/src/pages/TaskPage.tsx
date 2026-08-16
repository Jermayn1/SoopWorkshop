import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import {
  AlertCircle,
  CheckCircle,
  FileCode2,
  FileText,
  Loader2,
  RefreshCw,
  Upload,
  X,
} from 'lucide-react'
import ReactMarkdown from 'react-markdown'
import { HintPanel } from '../components/HintPanel'
import { createSubmission, fetchTask } from '../api/endpoints'
import { UPLOAD_LIMITS, checkFiles, formatBytes } from '../api/uploadLimits'
import type { Difficulty, Task } from '../api/types'

const DIFFICULTY_LABELS: Record<Difficulty, string> = {
  Easy: 'Leicht',
  Medium: 'Mittel',
  Hard: 'Schwer',
}

const DIFFICULTY_CLASSES: Record<Difficulty, string> = {
  Easy: 'bg-emerald-100 text-emerald-800',
  Medium: 'bg-amber-100 text-amber-800',
  Hard: 'bg-rose-100 text-rose-800',
}

type LoadState =
  | { kind: 'loading' }
  | { kind: 'ok'; task: Task }
  | { kind: 'missing'; message: string }
  | { kind: 'unreachable'; message: string }

export function TaskPage() {
  const { taskId = '' } = useParams()
  const navigate = useNavigate()

  const [state, setState] = useState<LoadState>({ kind: 'loading' })
  const [attempt, setAttempt] = useState(0)
  const [files, setFiles] = useState<File[]>([])
  const [rejections, setRejections] = useState<string[]>([])
  const [serverError, setServerError] = useState<string | null>(null)
  const [sending, setSending] = useState(false)
  const [dragging, setDragging] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    const controller = new AbortController()
    setState({ kind: 'loading' })
    setFiles([])
    setRejections([])
    setServerError(null)

    fetchTask(taskId, controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return
        if (result.kind === 'ok') setState({ kind: 'ok', task: result.value })
        else if (result.kind === 'notFound') setState({ kind: 'missing', message: result.message })
        else setState({ kind: 'unreachable', message: result.message })
      })
      .catch((cause) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') return
        setState({ kind: 'unreachable', message: 'Die Aufgabe konnte nicht geladen werden.' })
      })

    return () => controller.abort()
  }, [taskId, attempt])

  const addFiles = useCallback(
    (incoming: FileList | null) => {
      if (!incoming || incoming.length === 0) return
      const { accepted, rejections: reasons } = checkFiles(files, Array.from(incoming))
      setFiles(accepted)
      setRejections(reasons)
      setServerError(null)
    },
    [files],
  )

  const submit = useCallback(async () => {
    if (files.length === 0 || sending) return
    setSending(true)
    setServerError(null)

    const result = await createSubmission(taskId, files)
    setSending(false)

    if (result.kind === 'ok') {
      navigate(`/abgaben/${result.value.id}`)
      return
    }

    // Der Server antwortet mit fertigen deutschen Saetzen. Die werden
    // durchgereicht, nicht durch eine eigene Meldung ersetzt.
    setServerError(result.message)
  }, [files, navigate, sending, taskId])

  if (state.kind === 'loading') {
    return (
      <div className="flex-1 overflow-y-auto bg-white p-8">
        <div className="max-w-4xl mx-auto w-full space-y-6" aria-hidden="true">
          <div className="h-6 w-40 rounded-full bg-slate-200 animate-pulse" />
          <div className="h-10 w-2/3 rounded bg-slate-200 animate-pulse" />
          <div className="h-40 rounded-2xl bg-slate-100 animate-pulse" />
        </div>
      </div>
    )
  }

  if (state.kind !== 'ok') {
    const unreachable = state.kind === 'unreachable'
    return (
      <div className="flex-1 flex items-center justify-center bg-white p-8">
        <div className="max-w-md text-center">
          <div className="w-16 h-16 bg-slate-100 rounded-2xl flex items-center justify-center mx-auto mb-5">
            <AlertCircle className="w-8 h-8 text-slate-500" aria-hidden="true" />
          </div>
          <h2 className="text-2xl font-bold text-slate-800 mb-2">
            {unreachable ? 'Der Server antwortet nicht' : 'Diese Aufgabe gibt es nicht'}
          </h2>
          <p className="text-slate-600">{state.message}</p>
          {unreachable && (
            <button
              type="button"
              onClick={() => setAttempt((n) => n + 1)}
              className="mt-5 inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white hover:bg-indigo-700 transition-colors"
            >
              <RefreshCw className="w-4 h-4" aria-hidden="true" />
              Erneut versuchen
            </button>
          )}
        </div>
      </div>
    )
  }

  const { task } = state
  const totalBytes = files.reduce((sum, f) => sum + f.size, 0)
  const hasContract = task.expectedClassName !== null || task.expectedMethods.length > 0

  return (
    <div className="flex-1 bg-white flex flex-col p-8 overflow-y-auto">
      <div className="max-w-4xl mx-auto w-full pb-20">
        <header className="mb-10 border-b pb-8 border-slate-100">
          <div className="flex items-center gap-2 mb-3 flex-wrap">
            <span
              className={`px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider ${DIFFICULTY_CLASSES[task.difficulty]}`}
            >
              {DIFFICULTY_LABELS[task.difficulty]}
            </span>
            <span className="px-3 py-1 bg-slate-100 text-slate-700 rounded-full text-xs font-bold uppercase tracking-wider">
              {task.evaluationMode === 'ConsoleOnly'
                ? 'Konsole'
                : task.evaluationMode === 'UnitTestOnly'
                  ? 'Unit-Tests'
                  : 'Konsole + Unit-Tests'}
            </span>
          </div>
          <h1 className="text-4xl font-extrabold text-slate-900 leading-tight">{task.title}</h1>
        </header>

        <section className="prose prose-slate max-w-none mb-10 prose-p:text-slate-700 prose-p:leading-relaxed prose-code:before:content-none prose-code:after:content-none prose-code:rounded prose-code:bg-slate-100 prose-code:px-1.5 prose-code:py-0.5 prose-code:font-normal">
          <div className="bg-slate-50/50 p-8 rounded-2xl border border-slate-100 shadow-sm">
            <ReactMarkdown>{task.description}</ReactMarkdown>
          </div>
        </section>

        {/* Der Aufgaben-Vertrag wurde bisher nie angezeigt — der ContractChecker
            bewertete also gegen eine Vorgabe, die der Teilnehmer nicht lesen
            konnte. */}
        {hasContract && (
          <section className="mb-10">
            <h2 className="text-sm font-bold uppercase tracking-wider text-slate-500 mb-3">
              Was geprueft wird
            </h2>
            <dl className="rounded-2xl border border-slate-200 bg-white divide-y divide-slate-100">
              {task.expectedClassName && (
                <div className="flex flex-col sm:flex-row gap-1 sm:gap-4 px-5 py-3">
                  <dt className="text-sm text-slate-500 sm:w-40 shrink-0">Klasse</dt>
                  <dd className="font-mono text-sm text-slate-900">{task.expectedClassName}</dd>
                </div>
              )}
              {task.expectedMethods.length > 0 && (
                <div className="flex flex-col sm:flex-row gap-1 sm:gap-4 px-5 py-3">
                  <dt className="text-sm text-slate-500 sm:w-40 shrink-0">Methoden</dt>
                  <dd className="font-mono text-sm text-slate-900 space-y-1 min-w-0">
                    {task.expectedMethods.map((signature) => (
                      <div key={signature} className="break-words">
                        {signature}
                      </div>
                    ))}
                  </dd>
                </div>
              )}
            </dl>
          </section>
        )}

        {task.visibleUnitTestFiles.length > 0 && (
          <section className="mb-10">
            <h2 className="text-sm font-bold uppercase tracking-wider text-slate-500 mb-3">
              Diese Tests laufen gegen deine Abgabe
            </h2>
            <div className="space-y-3">
              {task.visibleUnitTestFiles.map((file) => (
                <details
                  key={file.id}
                  className="rounded-2xl border border-slate-200 overflow-hidden"
                >
                  <summary className="cursor-pointer px-5 py-3 font-mono text-sm bg-slate-50 hover:bg-slate-100">
                    {file.fileName}
                  </summary>
                  <pre className="overflow-x-auto bg-slate-800 p-4 text-xs leading-relaxed text-slate-100">
                    {file.content}
                  </pre>
                </details>
              ))}
            </div>
          </section>
        )}

        <section className="mb-10">
          <HintPanel hints={task.hints} />
        </section>

        <section>
          <div className="mb-4 flex items-center justify-between gap-4 flex-wrap">
            <h2 className="text-xl font-bold text-slate-800">Loesung abgeben</h2>
            {files.length > 0 && (
              <button
                type="button"
                onClick={() => {
                  setFiles([])
                  setRejections([])
                  setServerError(null)
                }}
                className="text-sm text-rose-700 font-semibold hover:underline"
              >
                Alle entfernen
              </button>
            )}
          </div>

          <div
            onDragOver={(event) => {
              event.preventDefault()
              setDragging(true)
            }}
            onDragLeave={() => setDragging(false)}
            onDrop={(event) => {
              event.preventDefault()
              setDragging(false)
              addFiles(event.dataTransfer.files)
            }}
            className={`relative border-2 border-dashed rounded-2xl p-12 flex flex-col items-center justify-center transition-all ${
              dragging
                ? 'bg-indigo-50 border-indigo-500'
                : files.length > 0
                  ? 'bg-emerald-50 border-emerald-300'
                  : 'bg-slate-50 border-slate-300'
            }`}
          >
            <input
              type="file"
              className="hidden"
              ref={inputRef}
              accept={UPLOAD_LIMITS.allowedExtension}
              multiple
              onChange={(event) => {
                addFiles(event.target.files)
                event.target.value = ''
              }}
            />

            <div className="w-16 h-16 bg-white rounded-2xl flex items-center justify-center shadow-lg border border-slate-100 mb-5">
              <Upload className="w-8 h-8 text-indigo-600" aria-hidden="true" />
            </div>
            <p className="text-lg font-bold text-slate-800 mb-1">
              Zieh deine .java-Dateien hierher
            </p>
            <button
              type="button"
              onClick={() => inputRef.current?.click()}
              className="text-indigo-700 font-semibold hover:underline"
            >
              oder klicke, um sie auszuwaehlen
            </button>

            {/* Die Grenzen stehen da, bevor jemand dagegen laeuft. */}
            <p className="mt-4 text-xs text-slate-600 text-center">
              {UPLOAD_LIMITS.allowedExtension} · hoechstens {UPLOAD_LIMITS.maxFileCount} Dateien ·{' '}
              {formatBytes(UPLOAD_LIMITS.maxFileSizeBytes)} je Datei ·{' '}
              {formatBytes(UPLOAD_LIMITS.maxTotalSizeBytes)} gesamt
            </p>
          </div>

          <AnimatePresence>
            {files.length > 0 && (
              <motion.ul
                initial={{ opacity: 0, y: -4 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0 }}
                className="mt-4 space-y-2"
              >
                {files.map((file) => (
                  <li
                    key={file.name}
                    className="flex items-center gap-3 rounded-xl border border-slate-200 bg-white px-4 py-2.5"
                  >
                    <FileCode2 className="w-4 h-4 text-slate-500 shrink-0" aria-hidden="true" />
                    <span className="font-mono text-sm text-slate-800 truncate flex-1">
                      {file.name}
                    </span>
                    <span className="text-xs text-slate-600 shrink-0">
                      {formatBytes(file.size)}
                    </span>
                    <button
                      type="button"
                      onClick={() => setFiles((current) => current.filter((f) => f !== file))}
                      aria-label={`${file.name} entfernen`}
                      className="p-1 rounded-md text-slate-500 hover:bg-slate-100 hover:text-rose-700"
                    >
                      <X className="w-4 h-4" aria-hidden="true" />
                    </button>
                  </li>
                ))}
                <li className="px-4 text-xs text-slate-600">
                  {files.length} von {UPLOAD_LIMITS.maxFileCount} Dateien ·{' '}
                  {formatBytes(totalBytes)} gesamt
                </li>
              </motion.ul>
            )}
          </AnimatePresence>

          {/* Eine verworfene Datei verschwindet nicht kommentarlos. */}
          {rejections.length > 0 && (
            <ul className="mt-4 space-y-1.5 rounded-xl border border-amber-200 bg-amber-50 p-4">
              {rejections.map((reason) => (
                <li key={reason} className="flex gap-2 text-sm text-amber-900">
                  <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" aria-hidden="true" />
                  {reason}
                </li>
              ))}
            </ul>
          )}

          <div className="mt-8 flex justify-center">
            <button
              type="button"
              disabled={files.length === 0 || sending}
              onClick={submit}
              className={`px-10 py-4 rounded-xl font-bold text-lg transition-all flex items-center gap-3 ${
                files.length === 0 || sending
                  ? 'bg-slate-200 text-slate-500 cursor-not-allowed'
                  : 'bg-indigo-600 text-white hover:bg-indigo-700 shadow-lg shadow-indigo-200'
              }`}
            >
              {sending ? (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" aria-hidden="true" />
                  Wird gesendet …
                </>
              ) : (
                <>
                  <CheckCircle className="w-5 h-5" aria-hidden="true" />
                  Jetzt pruefen
                </>
              )}
            </button>
          </div>

          {serverError && (
            <div
              role="alert"
              className="mt-4 flex gap-2 rounded-xl border border-rose-200 bg-rose-50 p-4 text-rose-800"
            >
              <FileText className="w-5 h-5 shrink-0" aria-hidden="true" />
              <p>{serverError}</p>
            </div>
          )}
        </section>
      </div>
    </div>
  )
}
