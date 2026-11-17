import { z } from 'zod'

// Mismas reglas que backend/src/TrabajoSocial.Application/Users/PasswordPolicy.cs: si cambian allá, cambiarlas aquí.
export const PASSWORD_MIN_LENGTH = 8
export const PASSWORD_MAX_LENGTH = 128

export const PASSWORD_HINT = `Mínimo ${PASSWORD_MIN_LENGTH} caracteres, con al menos una letra y un número, sin incluir el nombre de usuario.`

export const USERNAME_IN_PASSWORD = 'La contraseña no puede contener el nombre de usuario.'

export const passwordRule = z
  .string()
  .min(PASSWORD_MIN_LENGTH, `La contraseña debe tener al menos ${PASSWORD_MIN_LENGTH} caracteres.`)
  .max(PASSWORD_MAX_LENGTH, `Máximo ${PASSWORD_MAX_LENGTH} caracteres.`)
  .regex(/\p{L}/u, 'Debe incluir al menos una letra.')
  .regex(/\p{Nd}/u, 'Debe incluir al menos un número.')

/** La contraseña no puede contener el nombre de usuario (sin distinguir mayúsculas). */
export function containsUsername(password: string, username: string | null | undefined) {
  const name = username?.trim().toLowerCase()
  return Boolean(name) && password.toLowerCase().includes(name!)
}
