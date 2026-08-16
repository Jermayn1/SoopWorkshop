import { useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { ChevronDown, HelpCircle } from 'lucide-react'
import type { Hint } from '../api/types'

export function HintPanel({ hints }: { hints: Hint[] }) {
  const [isOpen, setIsOpen] = useState(false)

  if (hints.length === 0) return null

  return (
    <div className="bg-slate-50 border border-slate-200 rounded-xl overflow-hidden shadow-sm hover:shadow-md transition-all duration-300">
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
        className="w-full flex items-center justify-between p-5 hover:bg-slate-100 transition-colors"
      >
        <div className="flex items-center gap-3 font-semibold text-slate-800">
          <div className="w-8 h-8 bg-indigo-100 rounded-lg flex items-center justify-center">
            <HelpCircle className="w-5 h-5 text-indigo-700" />
          </div>
          Tipps &amp; Hilfestellungen
        </div>
        <motion.div animate={{ rotate: isOpen ? 180 : 0 }} transition={{ duration: 0.3 }}>
          <ChevronDown className="w-5 h-5 text-slate-500" aria-hidden="true" />
        </motion.div>
      </button>

      <AnimatePresence initial={false}>
        {isOpen && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.3, ease: 'easeInOut' }}
            className="overflow-hidden"
          >
            <div className="px-5 pb-5 border-t border-slate-100 pt-4">
              <ul className="space-y-3">
                {hints.map((hint, index) => (
                  <motion.li
                    key={hint.id}
                    initial={{ x: -10, opacity: 0 }}
                    animate={{ x: 0, opacity: 1 }}
                    transition={{ delay: index * 0.1 }}
                    className="flex gap-3 text-slate-700 leading-relaxed"
                  >
                    <span className="text-indigo-600 font-bold" aria-hidden="true">
                      •
                    </span>
                    {hint.content}
                  </motion.li>
                ))}
              </ul>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
