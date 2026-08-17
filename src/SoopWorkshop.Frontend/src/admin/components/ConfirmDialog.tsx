import { useEffect, useId, useRef } from 'react'
import { AlertTriangle, Loader2 } from 'lucide-react'

type ConfirmDialogProps = {
  title: string
  /** Was genau passiert. Bei Loeschungen gehoert der Umfang hier hinein. */
  message: string
  confirmLabel: string
  onConfirm: () => void
  onCancel: () => void
  busy?: boolean
}

// Bewusst das native <dialog> mit showModal() und nicht ein eigenes Overlay.
// Es bringt mit, was ein Dialog braucht und was nachgebaut regelmaessig
// schiefgeht: der Fokus bleibt darin gefangen, Escape schliesst, und der
// Hintergrund ist fuer Maus und Tastatur inert. Genau die Tastaturfalle, die
// in Phase 4 schon einmal in der Seitenleiste steckte.
export function ConfirmDialog({
  title,
  message,
  confirmLabel,
  onConfirm,
  onCancel,
  busy,
}: ConfirmDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const cancelRef = useRef<HTMLButtonElement>(null)
  const titleId = useId()

  // Der Escape-Listener wird genau einmal gehaengt, soll aber die aktuellen
  // Werte sehen. Ueber Refs statt ueber die Abhaengigkeitsliste, sonst wird der
  // Listener bei jedem Tastendruck des Elternteils neu gesetzt.
  const busyRef = useRef(busy)
  const onCancelRef = useRef(onCancel)
  busyRef.current = busy
  onCancelRef.current = onCancel

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return

    dialog.showModal()
    // Der Fokus startet auf "Abbrechen", nicht auf der Bestaetigung: ein
    // versehentliches Enter soll nichts loeschen.
    cancelRef.current?.focus()

    const schliessen = () => {
      if (!busyRef.current) onCancelRef.current()
    }

    // Zwei Wege zum selben Ziel, beide noetig:
    //
    // 1. "cancel" ist das native Ereignis beim Schliessen per Escape. Der
    //    Listener haengt von Hand und nicht als onCancel im JSX, weil "cancel"
    //    nicht blubbert und Reacts Ereignis-Delegation am Wurzelknoten es damit
    //    nie zu sehen bekommt — nachgemessen, der Dialog blieb offen.
    // 2. Zusaetzlich Escape direkt am Dialog. Chrome behandelt Escape fuer
    //    <dialog> ueber den CloseWatcher, und der springt nicht bei jeder
    //    Eingabequelle an. Der Aufruf ist doppelt abgesichert statt darauf zu
    //    vertrauen; onCancel zweimal aufzurufen schadet nicht, es setzt beide
    //    Male denselben Zustand.
    const onCancelEvent = (event: Event) => {
      event.preventDefault()
      schliessen()
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      event.preventDefault()
      schliessen()
    }

    dialog.addEventListener('cancel', onCancelEvent)
    dialog.addEventListener('keydown', onKeyDown)

    return () => {
      dialog.removeEventListener('cancel', onCancelEvent)
      dialog.removeEventListener('keydown', onKeyDown)
    }
  }, [])

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby={titleId}
      className="m-auto w-full max-w-md rounded-2xl border border-slate-200 bg-white p-0 shadow-2xl backdrop:bg-slate-900/40"
    >
      <div className="p-6">
        <div className="flex gap-4">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-amber-50 ring-1 ring-amber-200">
            <AlertTriangle className="w-5 h-5 text-amber-900" aria-hidden="true" />
          </div>
          <div className="min-w-0">
            <h2 id={titleId} className="text-lg font-bold text-slate-800">
              {title}
            </h2>
            <p className="mt-1 text-sm text-slate-600">{message}</p>
          </div>
        </div>

        <div className="mt-6 flex justify-end gap-3">
          <button
            ref={cancelRef}
            type="button"
            onClick={onCancel}
            disabled={busy}
            className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-60"
          >
            Abbrechen
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={busy}
            className="flex items-center gap-2 rounded-xl bg-rose-700 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-rose-800 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {busy && <Loader2 className="w-4 h-4 animate-spin" aria-hidden="true" />}
            {confirmLabel}
          </button>
        </div>
      </div>
    </dialog>
  )
}
