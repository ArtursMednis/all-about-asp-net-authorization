import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5113,
    // Forward API calls to ASP.NET Core. The browser only ever talks to this dev server,
    // so the app and the API share one origin: the cookie just works, no CORS needed.
    proxy: { '/api': 'http://localhost:5110' },
  },
})
