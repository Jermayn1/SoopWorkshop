import { Link } from 'react-router-dom'
import { ArrowLeft, LogOut } from 'lucide-react'
import { BrandMark } from '../../components/BrandMark'
import { useAdminOutlet } from '../adminOutlet'

// Platzhalter. Die eigentliche Verwaltung — Seitenleiste, Kategorien,
// Aufgaben-Editor — kommt in Etappe 5.1; hier steht vorerst nur, was den
// Zugangsschutz pruefbar macht.
export function AdminHomePage() {
  const { signOut } = useAdminOutlet()

  return (
    <div className="min-h-screen bg-slate-50">
      <header className="flex items-center gap-3 border-b border-slate-200 bg-white px-6 py-4">
        <BrandMark size={28} />
        <span className="font-bold text-slate-800">Soop Judge</span>
        <span className="rounded-md bg-slate-100 px-2 py-0.5 text-xs font-semibold uppercase tracking-wider text-slate-600">
          Verwaltung
        </span>

        <button
          type="button"
          onClick={signOut}
          className="ml-auto flex items-center gap-1.5 rounded-xl border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100"
        >
          <LogOut className="w-4 h-4" aria-hidden="true" />
          Abmelden
        </button>
      </header>

      <main className="mx-auto max-w-3xl p-8 anim-auf">
        <h1 className="text-2xl font-bold text-slate-800">Angemeldet</h1>
        <p className="mt-2 text-slate-600">
          Der Zugangsschutz steht. Kategorien, Aufgaben, Testfälle und der
          Bestands-Transfer folgen in den nächsten Etappen.
        </p>

        <Link
          to="/"
          className="mt-6 inline-flex items-center gap-1.5 text-sm font-medium text-slate-600 hover:text-slate-900 hover:underline"
        >
          <ArrowLeft className="w-4 h-4" aria-hidden="true" />
          Zurück zu den Aufgaben
        </Link>
      </main>
    </div>
  )
}
