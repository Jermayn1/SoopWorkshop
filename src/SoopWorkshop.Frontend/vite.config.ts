import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// Der Port ist derselbe, der im Backend unter Cors:AllowedOrigins steht.
// Wird er hier geaendert, muss er dort mitwandern — sonst blockt der Browser
// jede Anfrage, und der Fehler sieht nach einem kaputten Backend aus.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    strictPort: true,
  },
})
