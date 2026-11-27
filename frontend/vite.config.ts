import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  build: {
    rolldownOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes('node_modules')) return undefined
          if (id.includes('framer-motion') || id.includes('motion-dom')) {
            return 'vendor-motion'
          }
          if (id.includes('@radix-ui') || id.includes('@base-ui')) {
            return 'vendor-radix'
          }
          if (id.includes('@tanstack')) {
            return 'vendor-query'
          }
          if (
            id.includes('/react/') ||
            id.includes('/react-dom/') ||
            id.includes('react-router')
          ) {
            return 'vendor-react'
          }
          return undefined
        },
      },
    },
  },
})
