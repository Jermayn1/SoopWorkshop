import { defineConfig } from 'vite'
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
})
