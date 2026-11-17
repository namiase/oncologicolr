import { useId } from 'react'
import type { FieldError, UseFormRegisterReturn } from 'react-hook-form'
import { Field } from './Field'
import { describedBy } from './fieldStyles'
import { Input } from './Input'

interface PasswordFieldProps {
  label: string
  registration: UseFormRegisterReturn
  error?: FieldError
  hint?: string
  autoComplete?: string
  autoFocus?: boolean
}

/** Campo de contraseña con etiqueta, ayuda y error accesibles. */
export function PasswordField({ label, registration, error, hint, autoComplete = 'new-password', autoFocus }: PasswordFieldProps) {
  const id = useId()
  return (
    <Field id={id} label={label} required error={error?.message} hint={hint}>
      <Input
        id={id}
        type="password"
        autoComplete={autoComplete}
        autoFocus={autoFocus}
        invalid={Boolean(error)}
        aria-describedby={describedBy(id, error?.message, hint)}
        {...registration}
      />
    </Field>
  )
}
