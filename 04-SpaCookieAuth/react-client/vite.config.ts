import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5111,
    // Forward API calls to ASP.NET Core. The browser only ever talks to this dev server,
    // so the app and the API share one origin: the cookie just works, no CORS needed.
    proxy: { '/api': 'http://localhost:5110' },
  },
})
