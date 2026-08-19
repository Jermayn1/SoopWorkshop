import ReactMarkdown from 'react-markdown'

// Die Aufgabenbeschreibung als Markdown - an genau einer Stelle gesetzt.
//
// Teilnehmersicht (TaskView) und Vorschau im Aufgaben-Editor haben die
// prose-Klassen frueher jede fuer sich getragen und liefen damit auseinander:
// beim Teilnehmer verschwanden die Backticks um Inline-Code, in der Vorschau
// standen sie sichtbar im Text. Eine Vorschau, die etwas anderes zeigt als der
// Teilnehmer sieht, ist wertlos - deshalb dieselbe Komponente fuer beide.
//
// Zu den Klassen:
//
//   [&_:not(pre)>code] statt prose-code
//     "prose-code:" haengt an JEDEM <code>, auch dem in einem <pre>. Die
//     Pille fuer Inline-Code landete damit auf jedem Codeblock: eine Inline-Box
//     mit white-space: pre bricht in ein Fragment je Zeile, und jedes davon
//     zeichnet den Hintergrund selbst. Das ergab helle Kaestchen Zeile fuer
//     Zeile, darauf den hellen Text des dunklen Blocks. Dazu gilt das
//     padding-left nur beim ersten Fragment - eine ASCII-Pyramide stand
//     dadurch mit der Spitze 6px zu weit rechts.
//
//   prose-pre:leading-snug
//     Typography setzt Codebloecke auf 1,714. Ein Zeichen ist 0,6em breit,
//     eine Zeile war damit 2,86-mal so hoch wie breit - ASCII-Kunst wird so
//     um rund 40% ueberdehnt. 1,375 liegt zwischen Terminal (~1,2) und
//     Editor (~1,5): die Pyramide behaelt ihre Form, Java-Code bleibt luftig.
const KLASSEN = [
  'prose prose-slate max-w-none',
  'prose-p:text-slate-700 prose-p:leading-relaxed',
  'prose-pre:leading-snug',
  '[&_:not(pre)>code]:before:content-none [&_:not(pre)>code]:after:content-none',
  '[&_:not(pre)>code]:rounded [&_:not(pre)>code]:bg-slate-100',
  '[&_:not(pre)>code]:px-1.5 [&_:not(pre)>code]:py-0.5 [&_:not(pre)>code]:font-normal',
].join(' ')

export function TaskMarkdown({ children }: { children: string }) {
  return (
    <div className={KLASSEN}>
      <ReactMarkdown>{children}</ReactMarkdown>
    </div>
  )
}
