import { create } from 'zustand'
import type { AuthResponse, CurrentUser } from '@/types/api'

export type AuthStatus = 'checking' | 'authenticated' | 'anonymous'

interface AuthState {
  status: AuthStatus
  /** El access token vive solo en memoria; la sesión se restaura con la cookie HttpOnly de refresh. */
  accessToken: string | null
  /** Vencimiento del access token en hora local (ms), calculado con expiresInSeconds. */
  accessTokenExpiresAt: number
  /** Momento (ms) del último login/refresh: solo se vuelve a refrescar si hubo actividad después. */
  lastRefreshAt: number
  idleTimeoutMs: number
  user: CurrentUser | null
  /** Motivo del último cierre de sesión (p. ej. inactividad), para mostrarlo en el login. */
  logoutMessage: string | null
  setSession: (response: AuthResponse) => void
  clearSession: (message?: string | null) => void
  dismissLogoutMessage: () => void
}

export const DEFAULT_IDLE_TIMEOUT_MS = 30 * 60_000

export const useAuthStore = create<AuthState>()((set) => ({
  status: 'checking',
  accessToken: null,
  accessTokenExpiresAt: 0,
  lastRefreshAt: 0,
  idleTimeoutMs: DEFAULT_IDLE_TIMEOUT_MS,
  user: null,
  logoutMessage: null,
  setSession: ({ accessToken, expiresInSeconds, idleTimeoutMinutes, user }) => {
    const now = Date.now()
    set({
      status: 'authenticated',
      accessToken,
      accessTokenExpiresAt: now + expiresInSeconds * 1000,
      lastRefreshAt: now,
      idleTimeoutMs: idleTimeoutMinutes * 60_000,
      user,
      logoutMessage: null,
    })
  },
  clearSession: (message = null) =>
    set({ status: 'anonymous', accessToken: null, accessTokenExpiresAt: 0, lastRefreshAt: 0, user: null, logoutMessage: message }),
  dismissLogoutMessage: () => set({ logoutMessage: null }),
}))
