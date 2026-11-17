import { api } from '@/lib/api'
import { endLocalSession } from '@/lib/sessionSync'

export const IDLE_LOGOUT_MESSAGE = 'Su sesión se cerró por inactividad. Inicie sesión nuevamente.'

let ending = false

/**
 * Cierra la sesión en el servidor (revoca el refresh token e invalida el access token al
 * instante) y luego en todas las pestañas. Si el servidor no responde, igual se cierra localmente.
 */
export async function endSession(message: string | null = null) {
  if (ending) return
  ending = true
  try {
    await api.post('/auth/logout')
  } catch {
    /* sin conexión: la sesión del servidor vencerá sola por inactividad */
  } finally {
    endLocalSession(message)
    ending = false
  }
}
