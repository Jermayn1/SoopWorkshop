// Zustaende eines Speichervorgangs. Vier, nicht zwei — wie ueberall im Projekt
// (§6 "Zustaende statt Wahrheitswerte").
//
// "saved" ist eigen: es ist kein Dauerzustand, sondern eine kurze Rueckmeldung.
// Ohne ihn bleibt nach dem Speichern alles wie vorher stehen und man weiss
// nicht, ob der Klick angekommen ist.
export type SaveState =
  | { kind: 'idle' }
  | { kind: 'saving' }
  | { kind: 'saved' }
  | { kind: 'error'; message: string }
