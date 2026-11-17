import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig, type Plugin } from 'vite'

// En desarrollo, /api y /uploads se redirigen a la API .NET: mismo origen => sin CORS
// y la cookie HttpOnly del refresh token funciona sin configuración extra.
const apiTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5080'

/**
 * Content Security Policy: solo se ejecutan scripts propios (bloquea scripts inyectados / XSS).
 * 'unsafe-inline' en estilos lo requieren las notificaciones (sonner); no permite ejecutar código.
 */
const csp = [
  "default-src 'self'",
  "script-src 'self'",
  "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
  "font-src 'self' https://fonts.gstatic.com",
  "img-src 'self' data: blob:",
  "connect-src 'self'",
  "object-src 'none'",
  "base-uri 'self'",
  "form-action 'self'",
]

const securityHeaders = {
  'X-Content-Type-Options': 'nosniff',
  'X-Frame-Options': 'DENY',
  'Referrer-Policy': 'strict-origin-when-cross-origin',
  'Permissions-Policy': 'camera=(), microphone=(), geolocation=(), payment=()',
}

/** Inserta la CSP en el index.html del build de producción (en desarrollo rompería el HMR de Vite). */
function cspMetaTag(): Plugin {
  return {
    name: 'csp-meta-tag',
    apply: 'build',
    transformIndexHtml: () => [
      { tag: 'meta', attrs: { 'http-equiv': 'Content-Security-Policy', content: csp.join('; ') }, injectTo: 'head-prepend' },
    ],
  }
}

export default defineConfig({
  plugins: [react(), tailwindcss(), cspMetaTag()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  build: {
    rolldownOptions: {
      output: {
        // Librerías en chunks propios: cambian poco, así el navegador las reutiliza entre despliegues.
        codeSplitting: {
          groups: [
            { name: 'react', test: /node_modules[\\/](react|react-dom|react-router|scheduler)[\\/]/ },
            { name: 'vendor', test: /node_modules[\\/]/ },
          ],
        },
      },
    },
  },
  server: {
    port: 5173,
    // Si el 5173 está ocupado, Vite toma el siguiente puerto libre.
    headers: securityHeaders,
    proxy: {
      '/api': { target: apiTarget },
      '/uploads': { target: apiTarget },
    },
  },
  preview: {
    // "npm run preview" sirve el build con las mismas cabeceras que debe tener producción.
    headers: { ...securityHeaders, 'Content-Security-Policy': [...csp, "frame-ancestors 'none'"].join('; ') },
  },
})
