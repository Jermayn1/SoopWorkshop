import { useId } from 'react'

type BrandMarkProps = {
  size?: number
}

// Das Zeichen der Anwendung. Bewusst die einzige blaue Fläche im Produkt:
// es kennzeichnet die Marke, während Bernstein die Bedienung führt und
// Grün und Rot ausschließlich Bewertungen tragen.
//
// Dieselbe Form liegt als public/favicon.svg noch einmal als Datei — der
// Browser kann ein Favicon nicht aus einer React-Komponente beziehen.
export function BrandMark({ size = 28 }: BrandMarkProps) {
  // Bei mehreren Marken auf einer Seite dürfen sich die Verlaufs-IDs nicht
  // überschneiden, sonst zieht die zweite die Füllung der ersten.
  const gradientId = useId()

  return (
    <svg width={size} height={size} viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#3b82f6" />
          <stop offset="1" stopColor="#1e40af" />
        </linearGradient>
      </defs>
      <rect width="32" height="32" rx="8" fill={`url(#${gradientId})`} />
      <path
        d="M21.4 12.3C21.4 9.7 19 8.5 16 8.5C13 8.5 10.6 9.7 10.6 12.1C10.6 14.3 12.6 15.3 16 15.9C19.4 16.5 21.4 17.7 21.4 19.9C21.4 22.3 19 23.5 16 23.5C13 23.5 10.6 22.3 10.6 19.7"
        fill="none"
        stroke="#ffffff"
        strokeWidth="3.2"
        strokeLinecap="round"
      />
    </svg>
  )
}
