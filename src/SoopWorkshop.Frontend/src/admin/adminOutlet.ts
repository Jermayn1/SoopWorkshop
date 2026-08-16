import { useOutletContext } from 'react-router-dom'

// Der Anmeldezustand lebt in RequireAdmin, damit er genau einmal geprueft
// wird. Alles darunter kommt ueber den Outlet-Kontext daran — das spart einen
// eigenen React-Context fuer einen einzigen Wert.
export type AdminOutletContext = {
  signOut: () => Promise<void>
}

export function useAdminOutlet(): AdminOutletContext {
  return useOutletContext<AdminOutletContext>()
}
