// Spiegelt SoopWorkshop.Shared/Constants/SubmissionUploadLimits.cs.
//
// Doppelt gepflegt, weil das Frontend nicht mehr dieselbe Assembly benutzt.
// Das Frontend blockt frueh und begruendet, das Backend prueft verbindlich —
// wer hier etwas aendert, aendert es dort mit.
export const UPLOAD_LIMITS = {
  allowedExtension: '.java',
  maxFileCount: 10,
  maxFileSizeBytes: 1024 * 1024,
  maxTotalSizeBytes: 10 * 1024 * 1024,
} as const

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1).replace('.', ',')} MB`
}

// Prueft eine Auswahl gegen die Grenzen und liefert die Dateien, die
// uebernommen werden, samt Begruendung fuer jede verworfene.
//
// Eine verworfene Datei darf nicht kommentarlos verschwinden — im
// Referenzprojekt kam an dieser Stelle ein alert() mit einem Satz fuer alle
// Faelle, und mehrere Dateien waren gar nicht vorgesehen.
export function checkFiles(
  existing: File[],
  incoming: File[],
): { accepted: File[]; rejections: string[] } {
  const accepted: File[] = [...existing]
  const rejections: string[] = []

  for (const file of incoming) {
    if (!file.name.toLowerCase().endsWith(UPLOAD_LIMITS.allowedExtension)) {
      rejections.push(`„${file.name}“ ist keine ${UPLOAD_LIMITS.allowedExtension}-Datei.`)
      continue
    }

    // Gross- und Kleinschreibung zaehlt hier NICHT, weil sie es im Backend
    // nicht tut (SubmissionUploadValidator vergleicht mit OrdinalIgnoreCase).
    // Vorher liess der Browser 'A.java' neben 'a.java' durch, und erst der
    // Server lehnte ab - der Teilnehmer bekam eine Ablehnung fuer etwas, das
    // die Oberflaeche eben noch angenommen hatte. Bei Namensgleichheit
    // ueberschreibt javac ausserdem die eine Klasse mit der anderen.
    if (accepted.some((f) => f.name.toLowerCase() === file.name.toLowerCase())) {
      rejections.push(`„${file.name}“ ist bereits ausgewählt.`)
      continue
    }

    if (file.size === 0) {
      rejections.push(`„${file.name}“ ist leer.`)
      continue
    }

    if (file.size > UPLOAD_LIMITS.maxFileSizeBytes) {
      // Wortgleich mit SubmissionUploadValidator.cs — der Teilnehmer soll
      // denselben Satz lesen, egal ob der Browser oder der Server ablehnt.
      //
      // Hier stand vorher die tatsaechliche Groesse neben der erlaubten. Das
      // klang praeziser, war aber bei knappen Ueberschreitungen falsch: 1 MB
      // plus 10 Bytes rundet auf "1,0 MB", die Grenze ebenso — herauskam
      // "ist 1,0 MB gross - erlaubt sind 1,0 MB je Datei". Der Test prueft mit
      // 2 MB und hat das nie gesehen.
      rejections.push(
        `„${file.name}“ ist größer als ${UPLOAD_LIMITS.maxFileSizeBytes / 1024} KB.`,
      )
      continue
    }

    if (accepted.length >= UPLOAD_LIMITS.maxFileCount) {
      rejections.push(
        `„${file.name}“ passt nicht mehr dazu — es sind höchstens ${UPLOAD_LIMITS.maxFileCount} Dateien erlaubt.`,
      )
      continue
    }

    const total = accepted.reduce((sum, f) => sum + f.size, 0) + file.size
    if (total > UPLOAD_LIMITS.maxTotalSizeBytes) {
      rejections.push(
        `„${file.name}“ sprengt die Gesamtgröße von ${formatBytes(UPLOAD_LIMITS.maxTotalSizeBytes)}.`,
      )
      continue
    }

    accepted.push(file)
  }

  return { accepted, rejections }
}
