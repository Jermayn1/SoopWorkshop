import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// Der Port ist derselbe, der im Backend unter Cors:AllowedOrigins steht.
// Wird er hier geaendert, muss er dort mitwandern — sonst blockt der Browser
// jede Anfrage, und der Fehler sieht nach einem kaputten Backend aus.
export default defineConfig({
  plugins: [react(), tailwindcss()],

  // React genau einmal aufloesen. Ohne das zog die Vorbuendelung fuer motion
  // eine zweite React-Kopie herein; die Folge war "Invalid hook call" tief in
  // motion, und die Animationen blieben auf ihrem Startwert stehen — Abschnitte
  // mit initial={{opacity:0}} blieben damit dauerhaft unsichtbar.
  resolve: {
    dedupe: ['react', 'react-dom'],
  },

  server: {
    port: 5173,
    strictPort: true,
  },

  // Vitest teilt sich diese Datei mit dem Build, deshalb kommt defineConfig
  // oben aus 'vitest/config' und nicht aus 'vite'.
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],

    // Kein globals: true. Mit ausdruecklichen Importen aus 'vitest' braucht
    // tsconfig.app.json keinen zusaetzlichen types-Eintrag, und tsc -b prueft
    // die Tests einfach mit.
    globals: false,

    css: false,

    coverage: {
      provider: 'v8',
      reportsDirectory: '../../artifacts/coverage/frontend',
      reporter: ['text-summary', 'html'],

      // Ohne include zaehlt v8 nur Dateien, die ein Test importiert hat - eine
      // Zahl, die genau das ausblendet, was fehlt. So stehen ungetestete
      // Dateien mit 0 Prozent drin und die Quote sagt die Wahrheit.
      include: ['src/**/*.{ts,tsx}'],
      // Nur was auch Verhalten traegt. Erzeugtes, Einstiegspunkte und die Tests
      // selbst wuerden die Zahl bloss verwaessern.
      exclude: [
        'src/api/schema.d.ts',
        'src/main.tsx',
        'src/test/**',
        '**/*.test.ts',
        '**/*.test.tsx',
      ],
    },
  },
})
