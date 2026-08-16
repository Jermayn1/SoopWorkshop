import { useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { Check, ChevronDown, X } from 'lucide-react'
import type { CategoryResult, TestCaseResult } from '../api/types'

const CATEGORY_LABELS: Record<string, string> = {
  CleanCode: 'Clean Code',
  Compilability: 'Kompilierbarkeit',
  Functionality: 'Funktionalität',
  // Altlasten aus früheren Auswertungen — sie werden nicht mehr vergeben,
  // kommen in alten Ergebnissen aber noch vor und brauchen einen Namen.
  CharacterSet: 'Zeichensatz',
  NamingConventions: 'Namenskonventionen',
  TestCases: 'Testfälle',
  UnitTests: 'Unit-Tests',
}

// Zeigt eine Teilprüfung nach den Regeln aus CLAUDE.md §5.7:
// Eingabe nur wenn es eine gab, Erwartet und Erhalten immer gemeinsam,
// bestandene Prüfungen zeigen nichts.
function TestCaseRow({ test, index }: { test: TestCaseResult; index: number }) {
  const hasComparison = test.expectedOutput !== '' || test.actualOutput !== ''

  return (
    <motion.div
      initial={{ x: -8, opacity: 0 }}
      animate={{ x: 0, opacity: 1 }}
      transition={{ delay: index * 0.05 }}
      className={`flex gap-3 p-3 rounded-xl border text-sm ${
        test.passed ? 'bg-emerald-50 border-emerald-100' : 'bg-rose-50 border-rose-100'
      }`}
    >
      <div
        className={`mt-0.5 shrink-0 w-5 h-5 rounded-full flex items-center justify-center ${
          test.passed ? 'bg-emerald-600' : 'bg-rose-600'
        }`}
      >
        {test.passed ? (
          <Check className="w-3 h-3 text-white" strokeWidth={3} aria-hidden="true" />
        ) : (
          <X className="w-3 h-3 text-white" strokeWidth={3} aria-hidden="true" />
        )}
      </div>

      <div className="min-w-0 flex-1">
        <p className={`font-medium ${test.passed ? 'text-emerald-900' : 'text-rose-900'}`}>
          {test.description}
        </p>

        {/* Bestandene Prüfungen zeigen nichts weiter — die Zustimmung steht
            schon im Haken. */}
        {!test.passed && (
          <dl className="mt-2 grid grid-cols-[5.5rem_1fr] gap-x-3 gap-y-1 font-mono text-xs">
            {test.input !== '' && (
              <>
                <dt className="text-slate-600">Eingabe</dt>
                <dd className="text-slate-800 whitespace-pre-wrap break-words">{test.input}</dd>
              </>
            )}
            {hasComparison && (
              <>
                <dt className="text-slate-600">Erwartet</dt>
                <dd className="text-emerald-800 whitespace-pre-wrap break-words">
                  {test.expectedOutput || '—'}
                </dd>
                <dt className="text-slate-600">Erhalten</dt>
                <dd className="text-rose-800 whitespace-pre-wrap break-words">
                  {test.actualOutput || '—'}
                </dd>
              </>
            )}
          </dl>
        )}
      </div>
    </motion.div>
  )
}

export function CategoryCard({ result, delay }: { result: CategoryResult; delay: number }) {
  const [open, setOpen] = useState(!result.passed)
  const percentage = result.maxPoints > 0 ? Math.round((result.points / result.maxPoints) * 100) : 0
  const passedCount = result.testCaseResults.filter((t) => t.passed).length
  const total = result.testCaseResults.length
  const hasDetails = total > 0 || result.errorTip !== ''

  return (
    <motion.div
      initial={{ y: 16, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      transition={{ delay, ease: 'easeOut' }}
      className={`bg-white rounded-2xl border shadow-sm overflow-hidden transition-shadow ${
        open
          ? `${result.passed ? 'border-emerald-200' : 'border-rose-200'} shadow-md`
          : 'border-slate-200 hover:shadow-md'
      }`}
    >
      <button
        type="button"
        onClick={() => hasDetails && setOpen((o) => !o)}
        aria-expanded={hasDetails ? open : undefined}
        className={`w-full flex items-center gap-4 px-5 py-4 text-left transition-colors ${
          hasDetails ? 'hover:bg-slate-50/70 cursor-pointer' : 'cursor-default'
        }`}
      >
        <div className="flex-1 min-w-0">
          <p className="text-slate-600 text-xs font-semibold uppercase tracking-widest mb-1">
            {CATEGORY_LABELS[result.category] ?? result.category}
          </p>
          <div className="flex items-center gap-3">
            <span className="text-lg font-bold text-slate-800 tabular-nums">
              {result.points}
              <span className="text-slate-500 text-sm font-normal"> / {result.maxPoints}</span>
            </span>
            <div className="flex-1 h-1.5 bg-slate-100 rounded-full overflow-hidden">
              <motion.div
                initial={{ width: 0 }}
                animate={{ width: `${percentage}%` }}
                transition={{ duration: 1, delay: delay + 0.3 }}
                className={`h-full ${
                  result.passed
                    ? 'bg-gradient-to-r from-emerald-400 to-emerald-600'
                    : 'bg-gradient-to-r from-rose-400 to-rose-600'
                }`}
              />
            </div>
            {total > 0 && (
              <span className="text-xs font-semibold text-slate-700 tabular-nums shrink-0">
                {passedCount}/{total}
              </span>
            )}
          </div>
        </div>

        {hasDetails && (
          <motion.div
            animate={{ rotate: open ? 180 : 0 }}
            transition={{ duration: 0.2 }}
            className="text-slate-500 shrink-0"
          >
            <ChevronDown className="w-5 h-5" aria-hidden="true" />
          </motion.div>
        )}
      </button>

      <AnimatePresence initial={false}>
        {open && hasDetails && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.25, ease: 'easeInOut' }}
            className="overflow-hidden"
          >
            <div className="px-5 pb-5 pt-4 border-t border-slate-100 space-y-2">
              {result.errorTip !== '' && (
                <p className="rounded-xl border border-slate-200 bg-slate-50 p-3 text-sm text-slate-700 whitespace-pre-wrap">
                  {result.errorTip}
                </p>
              )}
              {result.testCaseResults.map((test, index) => (
                <TestCaseRow key={test.id} test={test} index={index} />
              ))}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  )
}
