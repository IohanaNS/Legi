import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      "/api/v1/identity": { target: "http://localhost:5000", changeOrigin: true },
      "/api/v1/catalog": { target: "http://localhost:5112", changeOrigin: true },
      "/api/v1/library": { target: "http://localhost:5200", changeOrigin: true },
      "/api/v1/social": { target: "http://localhost:5300", changeOrigin: true },
      "/media": {
        target: "http://localhost:9000",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/media/, "/legi-media"),
      },
    },
  },
})
