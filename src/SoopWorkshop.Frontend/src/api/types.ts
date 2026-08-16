import type { components } from './schema'

// Die erzeugten Typen aus schema.d.ts sind der Vertrag — aber ein unbequemer:
// .NET setzt kein "required", also ist dort jedes Feld optional, und jedes int
// kommt als "number | string" heraus, weil ASP.NET beim Binden auch Zahlen als
// Zeichenkette annimmt. Wuerde die Oberflaeche direkt damit arbeiten, stuende
// in jeder Komponente ein "?? ''".
//
// Deshalb hier einmal saubere Typen und daneben (mappers.ts) die Umsetzung.
// Der Vertrag bleibt die Quelle: verschwindet ein Feld im Backend, bricht die
// Umsetzung beim Uebersetzen — genau die Absicherung, die wir wollten.

type Schemas = components['schemas']

export type Difficulty = NonNullable<Schemas['Difficulty']>
export type EvaluationMode = NonNullable<Schemas['EvaluationMode']>
export type SubmissionStatus = NonNullable<Schemas['SubmissionStatus']>
export type EvaluationCategory = NonNullable<Schemas['EvaluationCategory']>

export type Hint = {
  id: string
  content: string
  order: number
}

export type UnitTestFile = {
  id: string
  fileName: string
  content: string
  order: number
  /**
   * Ob der Teilnehmer die Datei sehen darf. Auf dem oeffentlichen Weg ist das
   * immer true — die API liefert dort nur freigeschaltete Dateien aus. Erst in
   * der Verwaltung kommen auch die verborgenen mit.
   */
  isVisibleToParticipant: boolean
}

// Konsolen-Testfall. Kommt ausschliesslich ueber die Admin-Endpunkte; die
// oeffentliche Aufgabe enthaelt ihn bewusst nicht, sonst stuende die Loesung
// in der Aufgabenstellung.
export type TaskTest = {
  id: string
  taskItemId: string
  input: string
  expectedOutput: string
  description: string
  order: number
}

// Aufgabenspezifisches Gewicht einer Bewertungskategorie. Kein Punktwert:
// die erreichbaren Punkte entstehen erst durch die Normierung auf 100.
export type TaskCategoryWeight = {
  id: string
  taskItemId: string
  category: EvaluationCategory
  weight: number
}

export type Task = {
  id: string
  categoryId: string
  title: string
  description: string
  difficulty: Difficulty
  order: number
  /** Ob die Aufgabe fuer Teilnehmer freigeschaltet ist. */
  isVisible: boolean
  evaluationMode: EvaluationMode
  /** Wie die Klasse heissen muss. null, wenn die Aufgabe nichts vorgibt. */
  expectedClassName: string | null
  /** Vollstaendige Signaturen zur Anzeige. */
  expectedMethods: string[]
  hints: Hint[]
  /** Nur die JUnit-Dateien, die der Admin freigeschaltet hat. */
  visibleUnitTestFiles: UnitTestFile[]
}

export type Category = {
  id: string
  name: string
  order: number
  /** Ob die Kategorie fuer Teilnehmer freigeschaltet ist. */
  isVisible: boolean
  tasks: Task[]
}

export type TestCaseResult = {
  id: string
  description: string
  /** Leer, wenn die Prüfung keine Eingabe hatte. */
  input: string
  expectedOutput: string
  actualOutput: string
  passed: boolean
  order: number
}

export type CategoryResult = {
  id: string
  category: EvaluationCategory
  passed: boolean
  points: number
  maxPoints: number
  errorTip: string
  testCaseResults: TestCaseResult[]
}

export type EvaluationResult = {
  id: string
  submissionId: string
  totalScore: number
  maxScore: number
  categoryResults: CategoryResult[]
}

export type Submission = {
  id: string
  taskItemId: string
  submittedAt: string
  status: SubmissionStatus
}

export type SubmissionState = {
  id: string
  /** Für den Zurück-Link zur richtigen Aufgabe. */
  taskItemId: string
  status: SubmissionStatus
  submittedAt: string
  /** Nur bei Status "Failed" gefuellt. */
  errorMessage: string
}
