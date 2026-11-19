import { useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { z } from 'zod'
import { Button } from '@/components/ui/Button'
import { Modal } from '@/components/ui/Modal'
import { PasswordField } from '@/components/ui/PasswordFields'
import { containsUsername, PASSWORD_HINT, passwordRule, USERNAME_IN_PASSWORD } from '@/features/users/passwordRules'
import { usersApi } from '@/features/users/usersApi'
import { refreshSession } from '@/lib/api'
import { getErrorMessage, getFieldErrors } from '@/lib/errors'
import { endSession } from './session'
import { useAuthStore } from '@/stores/authStore'

const buildSchema = (username: string | undefined) =>
  z
    .object({ currentPassword: z.string().min(1, 'Ingrese su contraseña actual.'), newPassword: passwordRule, confirmPassword: z.string() })
    .refine((v) => v.newPassword === v.confirmPassword, { path: ['confirmPassword'], message: 'Las contraseñas no coinciden.' })
    .refine((v) => v.newPassword !== v.currentPassword, { path: ['newPassword'], message: 'Debe ser distinta de la actual.' })
    .refine((v) => !containsUsername(v.newPassword, username), { path: ['newPassword'], message: USERNAME_IN_PASSWORD })

type ChangeForm = z.infer<ReturnType<typeof buildSchema>>

interface ChangePasswordDialogProps {
  onClose?: () => void
  /**
   * Contraseña asignada por un administrador: el cambio es obligatorio. El diálogo no se
   * puede cerrar (solo cerrar sesión) y, al guardar, se renueva el token para continuar.
   */
  required?: boolean
}

/** El usuario cambia su propia contraseña. Su sesión actual sigue abierta; las de otros equipos se cierran. */
export function ChangePasswordDialog({ onClose = () => {}, required = false }: ChangePasswordDialogProps) {
  const username = useAuthStore((s) => s.user?.username)
  const schema = useMemo(() => buildSchema(username), [username])
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ChangeForm>({
    resolver: zodResolver(schema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  })

  const submit = handleSubmit(async ({ currentPassword, newPassword }) => {
    try {
      await usersApi.changeOwnPassword(currentPassword, newPassword)
      if (required) {
        // El token actual aún dice "cambio pendiente": se pide uno nuevo (y el usuario actualizado).
        await refreshSession()
        toast.success('Contraseña actualizada. Ya puede usar el sistema.')
      } else {
        toast.success('Contraseña actualizada. Se cerraron sus sesiones en otros equipos.')
      }
      onClose()
    } catch (error) {
      const fields = getFieldErrors(error)
      if (fields.currentPassword) setError('currentPassword', { type: 'server', message: fields.currentPassword })
      else setError('newPassword', { type: 'server', message: fields.newPassword ?? getErrorMessage(error) })
    }
  })

  return (
    <Modal
      open
      onClose={onClose}
      title={required ? 'Cambie su contraseña' : 'Cambiar contraseña'}
      description={
        required ? 'Su contraseña fue asignada por un administrador. Por seguridad, elija una nueva para continuar.' : undefined
      }
      size="sm"
      dismissible={!required && !isSubmitting}
      footer={
        <>
          {required ? (
            <Button variant="secondary" onClick={() => void endSession()} disabled={isSubmitting}>
              Cerrar sesión
            </Button>
          ) : (
            <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
              Cancelar
            </Button>
          )}
          <Button type="submit" form="change-password-form" loading={isSubmitting}>
            Guardar
          </Button>
        </>
      }
    >
      <form id="change-password-form" onSubmit={submit} noValidate className="space-y-4">
        <PasswordField
          label="Contraseña actual"
          registration={register('currentPassword')}
          error={errors.currentPassword}
          autoComplete="current-password"
          autoFocus
        />
        <PasswordField label="Nueva contraseña" registration={register('newPassword')} error={errors.newPassword} hint={PASSWORD_HINT} />
        <PasswordField label="Confirmar nueva contraseña" registration={register('confirmPassword')} error={errors.confirmPassword} />
      </form>
    </Modal>
  )
}
