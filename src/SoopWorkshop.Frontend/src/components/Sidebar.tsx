import { BookOpen, Code, Layers, RefreshCw, Terminal } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { BrandMark } from './BrandMark'
import type { Category } from '../api/types'

function categoryIcon(name: string) {
  switch (name.toLowerCase()) {
    case 'grundlagen':
      return <Terminal className="w-4 h-4" />
    case 'oop':
      return <Layers className="w-4 h-4" />
    case 'arrays':
      return <Code className="w-4 h-4" />
    default:
      return <BookOpen className="w-4 h-4" />
  }
}

type SidebarProps = {
  categories: Category[]
  loading: boolean
  error: string | null
  onRetry: () => void
}

export function Sidebar({ categories, loading, error, onRetry }: SidebarProps) {
  return (
    <div className="w-64 shrink-0 bg-slate-50 h-screen border-r border-slate-200 flex flex-col overflow-y-auto">
      <div className="p-6 border-b border-slate-200 bg-white">
        <NavLink to="/" className="text-xl font-bold text-slate-800 flex items-center gap-2">
          <BrandMark size={32} />
          Soop Judge
        </NavLink>
      </div>

      <nav className="p-4 space-y-8">
        {loading && (
          <div className="space-y-2" aria-hidden="true">
            {[0, 1, 2].map((i) => (
              <div key={i} className="h-8 rounded-md bg-slate-200 animate-pulse" />
            ))}
          </div>
        )}

        {/* Ein Fehler in der Navigation darf den Rest der Seite nicht mitreißen —
            und "nicht erreichbar" steht hier im Wortlaut, statt als "keine
            Aufgaben vorhanden" verkleidet zu werden. */}
        {error && !loading && (
          <div className="rounded-lg border border-rose-200 bg-rose-50 p-3">
            <p className="text-sm text-rose-800">{error}</p>
            <button
              type="button"
              onClick={onRetry}
              className="mt-2 flex items-center gap-1.5 text-sm font-semibold text-rose-800 hover:underline"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              Erneut versuchen
            </button>
          </div>
        )}

        {!loading &&
          !error &&
          categories.map((category) => (
            <div key={category.id}>
              <h2 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3 px-3 flex items-center gap-2">
                {categoryIcon(category.name)}
                {category.name}
              </h2>
              <div className="space-y-1">
                {category.tasks.map((task) => (
                  <NavLink
                    key={task.id}
                    to={`/aufgaben/${task.id}`}
                    className={({ isActive }) =>
                      `block w-full text-left px-3 py-2 rounded-md text-sm font-medium transition-all ${
                        isActive
                          ? 'bg-indigo-600 text-white shadow-md shadow-indigo-100'
                          : 'text-slate-600 hover:bg-slate-200'
                      }`
                    }
                  >
                    {task.title}
                  </NavLink>
                ))}
                {category.tasks.length === 0 && (
                  <p className="px-3 py-2 text-sm text-slate-500 italic">
                    Noch keine Aufgaben freigeschaltet.
                  </p>
                )}
              </div>
            </div>
          ))}

        {!loading && !error && categories.length === 0 && (
          <p className="px-3 py-4 text-slate-500 text-sm italic">
            Es sind noch keine Aufgaben sichtbar geschaltet.
          </p>
        )}
      </nav>
    </div>
  )
}
