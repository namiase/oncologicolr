import { useEffect, useState } from 'react'
import { Clock } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Modal } from '@/components/ui/Modal'
import { endSession, IDLE_LOGOUT_MESSAGE } from '@/features/auth/session'
import { refreshSession } from '@/lib/api'
import { getLastActivity, markActivity } from '@/lib/sessionSync'
import { useAuthStore } from '@/stores/authStore'

/** Eventos que cuentan como actividad. Se escuchan localmente: no generan tráfico al API. */
const ACTIVITY_EVENTS = ['pointerdown', 'pointermove', 'keydown', 'wheel', 'touchstart', 'scroll'] as const
/** Aviso previo al cierre por inactividad. */
const WARNING_MS = 2 * 60_000
/** Se renueva el token cuando le queda menos de 1 minuto... y solo si hubo actividad. */
const REFRESH_MARGIN_MS = 60_000

/**
 * Vigila la sesión mientras el usuario está dentro del sistema:
 * - Cierra la sesión tras N minutos sin actividad (valor que define el servidor, 30 por defecto),
 *   con un aviso 2 minutos antes.
 * - Renueva el access token UNA vez por cada vida del token (15 min) y SOLO si el usuario tuvo
 *   actividad desde la última renovación. Una pestaña inactiva no hace ninguna llamada al API.
 */
export function SessionWatcher() {
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null)

  useEffect(() => {
    let lastEvent = 0
    const onActivity = () => {
      const now = Date.now()
      if (now - lastEvent < 1000) return
      lastEvent = now
      markActivity()
    }
    for (const name of ACTIVITY_EVENTS) window.addEventListener(name, onActivity, { passive: true, capture: true })

    // Reloj local (no consulta al API): evalúa inactividad y si toca renovar el token.
    const timer = window.setInterval(() => {
      const { status, idleTimeoutMs, accessTokenExpiresAt, lastRefreshAt } = useAuthStore.getState()
      if (status !== 'authenticated') return

      const now = Date.now()
      const lastActivity = getLastActivity()
      const remaining = idleTimeoutMs - (now - lastActivity)

      if (remaining <= 0) {
        setSecondsLeft(null)
        void endSession(IDLE_LOGOUT_MESSAGE)
        return
      }

      setSecondsLeft(remaining <= WARNING_MS ? Math.ceil(remaining / 1000) : null)

      if (accessTokenExpiresAt - now <= REFRESH_MARGIN_MS && lastActivity > lastRefreshAt) {
        void refreshSession()
      }
    }, 1000)

    return () => {
      window.clearInterval(timer)
      for (const name of ACTIVITY_EVENTS) window.removeEventListener(name, onActivity, { capture: true })
    }
  }, [])

  if (secondsLeft === null) return null

  const minutes = Math.floor(secondsLeft / 60)
  const seconds = String(secondsLeft % 60).padStart(2, '0')

  return (
    <Modal
      open
      onClose={() => markActivity(true)}
      title="¿Sigue ahí?"
      size="sm"
      footer={
        <>
          <Button variant="secondary" onClick={() => void endSession()}>
            Cerrar sesión
          </Button>
          <Button onClick={() => markActivity(true)} autoFocus>
            Continuar conectado
          </Button>
        </>
      }
    >
      <div className="flex gap-3">
        <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-amber-50 text-amber-600 dark:bg-amber-500/10 dark:text-amber-400">
          <Clock className="size-5" aria-hidden />
        </span>
        <p className="pt-1 text-sm text-slate-600" aria-live="polite">
          Por seguridad, su sesión se cerrará por inactividad en{' '}
          <span className="font-semibold text-slate-900 tabular-nums">
            {minutes}:{seconds}
          </span>
          .
        </p>
      </div>
    </Modal>
  )
}
