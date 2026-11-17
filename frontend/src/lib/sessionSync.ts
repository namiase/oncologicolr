import { queryClient } from '@/app/queryClient'
import { DEFAULT_IDLE_TIMEOUT_MS, useAuthStore } from '@/stores/authStore'
import type { AuthResponse } from '@/types/api'

/**
 * Estado de sesión compartido entre pestañas, sin llamadas al API:
 * - Última actividad del usuario en localStorage (solo una marca de tiempo, nada secreto).
 * - BroadcastChannel para que un login, refresh o logout en una pestaña se aplique a todas
 *   (así solo UNA pestaña renueva el token, no cada una por su lado).
 */
const ACTIVITY_KEY = 'ts:last-activity'
const IDLE_KEY = 'ts:idle-timeout-ms'
const WRITE_THROTTLE_MS = 5_000

let lastActivityInMemory = 0
let lastWrite = 0

const storage = {
  get: (key: string) => {
    try {
      return localStorage.getItem(key)
    } catch {
      return null
    }
  },
  set: (key: string, value: string) => {
    try {
      localStorage.setItem(key, value)
    } catch {
      /* almacenamiento no disponible (modo privado): se usa solo la memoria */
    }
  },
  remove: (key: string) => {
    try {
      localStorage.removeItem(key)
    } catch {
      /* idem */
    }
  },
}

/** Registra actividad del usuario (mouse, teclado, scroll, touch). Operación local. */
export function markActivity(force = false) {
  const now = Date.now()
  lastActivityInMemory = now
  if (force || now - lastWrite >= WRITE_THROTTLE_MS) {
    lastWrite = now
    storage.set(ACTIVITY_KEY, String(now))
  }
}

/** Última actividad en cualquier pestaña de este navegador (0 si no hay registro). */
export function getLastActivity(): number {
  const stored = Number(storage.get(ACTIVITY_KEY))
  return Math.max(lastActivityInMemory, Number.isFinite(stored) ? stored : 0)
}

export function getRememberedIdleTimeout(): number {
  const stored = Number(storage.get(IDLE_KEY))
  return Number.isFinite(stored) && stored > 0 ? stored : DEFAULT_IDLE_TIMEOUT_MS
}

type SessionMessage = { type: 'session'; data: AuthResponse } | { type: 'logout'; message: string | null }

const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel('ts-session')

/** Aplica una sesión nueva (login o refresh) y la comparte con las demás pestañas. */
export function applySession(data: AuthResponse, { broadcast }: { broadcast: boolean }) {
  useAuthStore.getState().setSession(data)
  storage.set(IDLE_KEY, String(data.idleTimeoutMinutes * 60_000))
  if (broadcast) channel?.postMessage({ type: 'session', data } satisfies SessionMessage)
}

/** Limpia la sesión local (y en las demás pestañas). No llama al API. */
export function endLocalSession(message: string | null, { broadcast = true } = {}) {
  useAuthStore.getState().clearSession(message)
  lastActivityInMemory = 0
  storage.remove(ACTIVITY_KEY)
  queryClient.clear()
  if (broadcast) channel?.postMessage({ type: 'logout', message } satisfies SessionMessage)
}

channel?.addEventListener('message', (event: MessageEvent<SessionMessage>) => {
  const message = event.data
  if (message.type === 'session') {
    applySession(message.data, { broadcast: false })
  } else if (message.type === 'logout' && useAuthStore.getState().status !== 'anonymous') {
    endLocalSession(message.message, { broadcast: false })
  }
})
