import axios, { isAxiosError, type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '@/stores/authStore'
import type { AuthResponse, ProblemDetails } from '@/types/api'
import { applySession, endLocalSession } from './sessionSync'

/** Cliente HTTP de la API. Adjunta el JWT y lo renueva cuando está por vencer. */
export const api = axios.create({ baseURL: '/api', withCredentials: true })

/** Si al token le quedan menos de 30 s, se renueva antes de enviar la petición (evita un 401). */
const PROACTIVE_REFRESH_MS = 30_000
/** Tras un fallo de red al renovar, no se reintenta hasta pasado este tiempo (sin tormenta de reintentos). */
const NETWORK_RETRY_MS = 30_000

// Solo las llamadas que crean o cierran la sesión se excluyen de la renovación automática;
// el resto de /auth (me, change-password) necesita un token vigente como cualquier otra.
const SESSION_CALLS = ['/auth/login', '/auth/refresh', '/auth/logout']
const isAuthCall = (url?: string) => SESSION_CALLS.some((path) => url?.startsWith(path))

let refreshInFlight: Promise<string | null> | null = null
let retryAfter = 0

/**
 * Renueva el access token con la cookie HttpOnly. Es "single-flight": varias llamadas
 * simultáneas comparten una sola petición a /auth/refresh.
 * - 401: la sesión terminó (logout, inactividad, vencimiento) → se cierra localmente.
 * - Error de red: se conserva la sesión y se reintenta más tarde.
 */
export function refreshSession({ silent = false }: { silent?: boolean } = {}): Promise<string | null> {
  if (Date.now() < retryAfter) return Promise.resolve(null)

  refreshInFlight ??= axios
    .post<AuthResponse>('/api/auth/refresh', null, { withCredentials: true })
    .then(({ data }) => {
      applySession(data, { broadcast: true })
      return data.accessToken
    })
    .catch((error: unknown) => {
      if (isAxiosError<ProblemDetails>(error) && error.response?.status === 401) {
        endLocalSession(silent ? null : (error.response.data?.detail ?? 'La sesión expiró. Inicie sesión nuevamente.'))
      } else {
        retryAfter = Date.now() + NETWORK_RETRY_MS
        // Al abrir la app sin conexión con el servidor no hay sesión que restaurar.
        if (useAuthStore.getState().status === 'checking') useAuthStore.getState().clearSession(null)
      }
      return null
    })
    .finally(() => {
      refreshInFlight = null
    })

  return refreshInFlight
}

api.interceptors.request.use(async (config) => {
  const { accessToken, accessTokenExpiresAt } = useAuthStore.getState()
  let token = accessToken

  if (token && !isAuthCall(config.url) && accessTokenExpiresAt - Date.now() < PROACTIVE_REFRESH_MS) {
    token = (await refreshSession()) ?? token
  }

  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean }

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetriableConfig | undefined

    // Respaldo: si aun así llega un 401 (p. ej. reloj desfasado), se renueva y reintenta una vez.
    if (error.response?.status === 401 && original && !original._retried && !isAuthCall(original.url)) {
      original._retried = true
      const token = await refreshSession()
      if (token) {
        original.headers.Authorization = `Bearer ${token}`
        return api(original)
      }
    }

    return Promise.reject(error)
  },
)
