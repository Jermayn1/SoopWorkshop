import { useOutletContext } from 'react-router-dom'
import type { Category } from '../api/types'

// Zwei Ebenen, zwei Kontexte.
//
// RequireAdmin haelt den Anmeldezustand, AdminLayout darunter den geladenen
// Bestand. Beide reichen ueber den Outlet-Kontext weiter — ein geschachtelter
// Outlet ERSETZT den Kontext seines Elters, deshalb nimmt AdminLayout signOut
// mit auf und gibt es weiter, statt es zu verlieren.

export type AdminSessionContext = {
  signOut: () => Promise<void>
}

export type AdminCatalogContext = AdminSessionContext & {
  categories: Category[]
  loading: boolean
  error: string | null
  /** Laedt den Bestand neu — nach dem Anlegen, Aendern oder Loeschen. */
  reload: () => void
}

export function useAdminSessionContext(): AdminSessionContext {
  return useOutletContext<AdminSessionContext>()
}

export function useAdminCatalog(): AdminCatalogContext {
  return useOutletContext<AdminCatalogContext>()
}
