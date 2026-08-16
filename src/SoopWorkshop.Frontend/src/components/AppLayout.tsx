import { useCallback, useEffect, useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { fetchCategories } from '../api/endpoints'
import type { Category } from '../api/types'

export function AppLayout() {
  const [categories, setCategories] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [attempt, setAttempt] = useState(0)

  const retry = useCallback(() => setAttempt((n) => n + 1), [])

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError(null)

    fetchCategories(controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return
        if (result.kind === 'ok') {
          setCategories(result.value)
        } else {
          setError(result.message)
        }
      })
      .catch((cause) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') return
        setError('Die Aufgabenliste konnte nicht geladen werden.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })

    return () => controller.abort()
  }, [attempt])

  return (
    <div className="flex w-full h-screen bg-white font-sans text-slate-900">
      <Sidebar categories={categories} loading={loading} error={error} onRetry={retry} />
      <main className="flex-1 flex flex-col min-h-0 overflow-hidden">
        <Outlet context={{ categories }} />
      </main>
    </div>
  )
}
