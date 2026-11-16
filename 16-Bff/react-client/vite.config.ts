import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5122,
    strictPort: true,
    // You open the app through the BFF (https://localhost:7122), which forwards to this dev server.
    // Hot reload has to connect through the BFF too.
    hmr: { clientPort: 7122 },
  },
  build: {
    // In production the BFF serves the built app itself.
    outDir: '../Bff/wwwroot',
    emptyOutDir: true,
  },
})
