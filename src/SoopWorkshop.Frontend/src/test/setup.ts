import '@testing-library/jest-dom/vitest'
import { afterEach, vi } from 'vitest'
import { cleanup } from '@testing-library/react'

// jsdom kennt matchMedia nicht. ResultView fragt darueber
// prefers-reduced-motion ab; ohne Stub wirft der erste Render.
//
// matches: true ist Absicht und nicht bloss der bequemere Wert: damit setzt
// useCountUp den Zielwert sofort statt ihn ueber requestAnimationFrame
// hochzuzaehlen, und der Punktestand steht ohne Warten im DOM. Bewegung laesst
// sich ohnehin nicht sinnvoll automatisiert pruefen (CLAUDE.md §6.1).
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: true,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  }),
})

// Ohne das teilen sich aufeinanderfolgende Tests denselben DOM-Baum, und ein
// getByText faende das Element aus dem vorherigen Test.
afterEach(() => {
  cleanup()
})
