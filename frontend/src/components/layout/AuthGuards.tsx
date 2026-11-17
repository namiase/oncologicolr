import { useEffect, type ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { ShieldX } from 'lucide-react'
import { EmptyState, FullScreenLoader } from '@/components/ui/Feedback'
import { usePermissions } from '@/features/auth/usePermissions'
import { endSession, IDLE_LOGOUT_MESSAGE } from '@/features/auth/session'
import { refreshSession } from '@/lib/api'
import { getLastActivity, getRememberedIdleTimeout, markActivity } from '@/lib/sessionSync'
import { useAuthStore } from '@/stores/authStore'

/**
 * Al cargar la app intenta restaurar la sesión con la cookie de refresh (HttpOnly).
 * Si la última actividad registrada supera el límite de inactividad (p. ej. se cerró la
 * pestaña y se volvió horas después), no se restaura: se cierra la sesión en el servidor.
 */
export function AuthBootstrap({ children }: { children: ReactNode }) {
  const status = useAuthStore((s) => s.status)

  useEffect(() => {
    if (useAuthStore.getState().status !== 'checking') return
    const lastActivity = getLastActivity()
    if (lastActivity > 0 && Date.now() - lastActivity >= getRememberedIdleTimeout()) {
      void endSession(IDLE_LOGOUT_MESSAGE)
      return
    }
    void refreshSession({ silent: true }).then((token) => {
      if (token) markActivity(true) // recargar la página es una acción del usuario
    })
  }, [])

  return status === 'checking' ? <FullScreenLoader /> : children
}

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const status = useAuthStore((s) => s.status)
  const location = useLocation()
  if (status !== 'authenticated') return <Navigate to="/login" replace state={{ from: location }} />
  return children
}

export function PublicOnlyRoute({ children }: { children: ReactNode }) {
  const status = useAuthStore((s) => s.status)
  if (status === 'authenticated') return <Navigate to="/inicio" replace />
  return children
}

/** Muestra la página solo si el perfil tiene permiso de ver la opción de menú. */
export function RequireMenu({ code, children }: { code: string; children: ReactNode }) {
  const { canView } = usePermissions(code)
  if (!canView) {
    return (
      <EmptyState
        icon={ShieldX}
        title="Acceso restringido"
        description="Su perfil no tiene permiso para ver esta sección. Si cree que es un error, contacte al administrador."
      />
    )
  }
  return children
}
