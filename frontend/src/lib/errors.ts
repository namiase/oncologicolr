import { isAxiosError } from 'axios'
import type { ProblemDetails } from '@/types/api'

export function getProblem(error: unknown): ProblemDetails | null {
  if (isAxiosError<ProblemDetails>(error) && error.response?.data && typeof error.response.data === 'object') {
    return error.response.data
  }
  return null
}

/** Mensaje legible para el usuario a partir de cualquier error de la API. */
export function getErrorMessage(error: unknown, fallback = 'Ocurrió un error inesperado.'): string {
  if (isAxiosError(error)) {
    if (!error.response) return 'No se pudo conectar con el servidor. Verifique su conexión.'
    if (error.response.status === 429) return 'Demasiados intentos. Espere un minuto e intente de nuevo.'
    if (error.response.status === 403) return 'No tiene permisos para realizar esta acción.'
  }
  const problem = getProblem(error)
  return problem?.detail ?? problem?.title ?? fallback
}

/**
 * Errores por campo (claves en camelCase). Combina los errores de validación (400)
 * y el campo de los conflictos (409).
 */
export function getFieldErrors(error: unknown): Record<string, string> {
  const problem = getProblem(error)
  const result: Record<string, string> = {}
  if (!problem) return result

  for (const [key, messages] of Object.entries(problem.errors ?? {})) {
    const name = key.replace(/^\$\./, '')
    const field = name.charAt(0).toLowerCase() + name.slice(1)
    if (messages.length > 0) result[field] = messages[0]
  }
  if (problem.field && problem.detail) result[problem.field] = problem.detail
  return result
}
