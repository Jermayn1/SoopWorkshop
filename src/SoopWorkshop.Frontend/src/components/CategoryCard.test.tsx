import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { CategoryCard } from './CategoryCard'
import { kategorie, teilpruefung } from '../test/fixtures'

function zeige(result: Parameters<typeof CategoryCard>[0]['result']) {
  return render(<CategoryCard result={result} delay={0} />)
}

// Die Regeln aus CLAUDE.md §5.7. Sie sind der Grund, warum die Ergebnisseite
// ueberhaupt verstaendlich ist - und sie stehen hier, nicht in ResultView.
describe('Darstellung einer Teilpruefung (§5.7)', () => {
  it('zeigt bei einer bestandenen Pruefung nur die Aussage', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({
            description: 'Das Programm gibt den Gruss aus',
            passed: true,
            input: 'egal',
            expectedOutput: 'Hallo',
            actualOutput: 'Hallo',
          }),
        ],
      }),
    )

    expect(screen.getByText('Das Programm gibt den Gruss aus')).toBeInTheDocument()

    // Bestandene Pruefungen zeigen nichts weiter - die Zustimmung steht schon
    // im Haken. Auch dann nicht, wenn Werte vorliegen.
    expect(screen.queryByText('Eingabe')).not.toBeInTheDocument()
    expect(screen.queryByText('Erwartet')).not.toBeInTheDocument()
    expect(screen.queryByText('Erhalten')).not.toBeInTheDocument()
  })

  it('zeigt die Eingabe nur, wenn es eine gab', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: false, input: '', expectedOutput: 'Hallo', actualOutput: 'Tach' }),
        ],
      }),
    )

    expect(screen.queryByText('Eingabe')).not.toBeInTheDocument()
    expect(screen.getByText('Erwartet')).toBeInTheDocument()
  })

  it('zeigt die Eingabe, wenn eine vorlag', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: false, input: '3 4', expectedOutput: '7', actualOutput: '12' }),
        ],
      }),
    )

    expect(screen.getByText('Eingabe')).toBeInTheDocument()
    expect(screen.getByText('3 4')).toBeInTheDocument()
  })

  // Ein "Erwartet" ohne Gegenstueck laesst den Leser raten. Die beiden gehoeren
  // zusammen, immer.
  it.each([
    ['nur Erwartet', 'Hallo', ''],
    ['nur Erhalten', '', 'Tach'],
    ['beides', 'Hallo', 'Tach'],
  ])('zeigt bei %s Erwartet UND Erhalten gemeinsam', (_fall, erwartet, erhalten) => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: false, expectedOutput: erwartet, actualOutput: erhalten }),
        ],
      }),
    )

    expect(screen.getByText('Erwartet')).toBeInTheDocument()
    expect(screen.getByText('Erhalten')).toBeInTheDocument()
  })

  // Fehlt eine Seite, steht dort ein Gedankenstrich - nicht nichts.
  it('setzt fuer die fehlende Seite einen Gedankenstrich', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: false, expectedOutput: 'Hallo', actualOutput: '' }),
        ],
      }),
    )

    expect(screen.getByText('—')).toBeInTheDocument()
  })

  // Ohne jede Erwartung gibt es nichts zu vergleichen; dann bleibt die Liste
  // leer, statt zweimal einen Gedankenstrich zu zeigen.
  it('zeigt ohne Erwartung gar keinen Vergleich', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({
            description: 'Der Code kompiliert',
            passed: false,
            expectedOutput: '',
            actualOutput: '',
          }),
        ],
      }),
    )

    expect(screen.getByText('Der Code kompiliert')).toBeInTheDocument()
    expect(screen.queryByText('Erwartet')).not.toBeInTheDocument()
    expect(screen.queryByText('—')).not.toBeInTheDocument()
  })
})

describe('CategoryCard', () => {
  it('uebersetzt die Kategorie ins Deutsche', () => {
    zeige(kategorie({ category: 'Compilability' }))

    expect(screen.getByText('Kompilierbarkeit')).toBeInTheDocument()
  })

  // Altlast-Kategorien kommen in alten Ergebnissen noch vor und brauchen einen
  // Namen, sonst stuende dort der englische Enum-Wert.
  it('kennt auch die abgeschafften Kategorien', () => {
    zeige(kategorie({ category: 'TestCases' }))

    expect(screen.getByText('Testfälle')).toBeInTheDocument()
  })

  it('zeigt Punkte, erreichbare Punkte und die Trefferquote', () => {
    zeige(
      kategorie({
        points: 40,
        maxPoints: 65,
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: true }),
          teilpruefung({ passed: false }),
          teilpruefung({ passed: false }),
        ],
      }),
    )

    expect(screen.getByText('40')).toBeInTheDocument()
    expect(screen.getByText('/ 65')).toBeInTheDocument()
    expect(screen.getByText('1/3')).toBeInTheDocument()
  })

  // Eine durchgefallene Kategorie steht offen da: dort will man sofort
  // nachsehen. Eine bestandene bleibt zu und haelt die Seite kurz.
  it('startet aufgeklappt, wenn die Kategorie durchgefallen ist', () => {
    zeige(kategorie({ passed: false, testCaseResults: [teilpruefung({ passed: false })] }))

    expect(screen.getByRole('button')).toHaveAttribute('aria-expanded', 'true')
  })

  it('startet zugeklappt, wenn alles bestanden ist', () => {
    zeige(kategorie({ passed: true, testCaseResults: [teilpruefung({ passed: true })] }))

    expect(screen.getByRole('button')).toHaveAttribute('aria-expanded', 'false')
  })

  it('laesst sich auf- und zuklappen', async () => {
    const user = userEvent.setup()
    zeige(kategorie({ passed: true, testCaseResults: [teilpruefung()] }))

    const knopf = screen.getByRole('button')
    await user.click(knopf)
    expect(knopf).toHaveAttribute('aria-expanded', 'true')

    await user.click(knopf)
    expect(knopf).toHaveAttribute('aria-expanded', 'false')
  })

  // Ohne Teilpruefungen und ohne Hinweis gibt es nichts aufzuklappen - dann
  // darf der Knopf auch nicht so tun, als gaebe es etwas.
  it('bietet ohne Inhalt kein Aufklappen an', () => {
    zeige(kategorie({ testCaseResults: [], errorTip: '' }))

    expect(screen.getByRole('button')).not.toHaveAttribute('aria-expanded')
  })

  it('klappt allein wegen eines Hinweises auf', () => {
    zeige(kategorie({ passed: false, testCaseResults: [], errorTip: 'Prüfe die Ausgabe.' }))

    expect(screen.getByRole('button')).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('Prüfe die Ausgabe.')).toBeInTheDocument()
  })

  // Was eingeklappt ist, bekommt inert - sonst bleiben die Elemente darin
  // antabbar, obwohl niemand sie sieht. Eine unsichtbare Tastaturfalle, in
  // Phase 4 schon einmal gefunden.
  it('macht den eingeklappten Bereich unerreichbar', () => {
    const { container } = zeige(
      kategorie({ passed: true, testCaseResults: [teilpruefung({ passed: true })] }),
    )

    const bereich = container.querySelector('[id^="kategorie-detail-"]')
    expect(bereich).not.toBeNull()
    expect(bereich).toHaveAttribute('inert')
  })

  it('nimmt dem aufgeklappten Bereich das inert wieder', async () => {
    const user = userEvent.setup()
    const { container } = zeige(
      kategorie({ passed: true, testCaseResults: [teilpruefung({ passed: true })] }),
    )

    await user.click(screen.getByRole('button'))

    expect(container.querySelector('[id^="kategorie-detail-"]')).not.toHaveAttribute('inert')
  })

  // Ohne die Abfrage teilte die Anzeige durch null.
  it('kommt mit null erreichbaren Punkten zurecht', () => {
    zeige(kategorie({ points: 0, maxPoints: 0 }))

    expect(screen.getByText('/ 0')).toBeInTheDocument()
  })
})
