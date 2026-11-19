import { useMemo } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { z } from 'zod'
import { Button } from '@/components/ui/Button'
import { Modal } from '@/components/ui/Modal'
import { PasswordField } from '@/components/ui/PasswordFields'
import { getErrorMessage, getFieldErrors } from '@/lib/errors'
import { containsUsername, PASSWORD_HINT, passwordRule, USERNAME_IN_PASSWORD } from './passwordRules'
import { usersApi } from './usersApi'
import type { UserItem } from './usersResource'

const buildSchema = (username: string) =>
  z
    .object({ newPassword: passwordRule, confirmPassword: z.string() })
    .refine((v) => v.newPassword === v.confirmPassword, { path: ['confirmPassword'], message: 'Las contraseñas no coinciden.' })
    .refine((v) => !containsUsername(v.newPassword, username), { path: ['newPassword'], message: USERNAME_IN_PASSWORD })

type ResetForm = z.infer<ReturnType<typeof buildSchema>>

interface ResetPasswordDialogProps {
  user: UserItem
  onClose: () => void
  /** Tras restablecer: la cuenta queda desbloqueada, así que la lista debe refrescarse. */
  onReset: () => void
}

/** El administrador asigna una contraseña nueva; la cuenta se desbloquea y sus sesiones se cierran. */
export function ResetPasswordDialog({ user, onClose, onReset }: ResetPasswordDialogProps) {
  const schema = useMemo(() => buildSchema(user.username), [user.username])
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ResetForm>({ resolver: zodResolver(schema), defaultValues: { newPassword: '', confirmPassword: '' } })

  const submit = handleSubmit(async ({ newPassword }) => {
    try {
      await usersApi.resetPassword(user.id, newPassword)
      toast.success(`Contraseña de ${user.username} restablecida. Deberá cambiarla al iniciar sesión.`)
      onReset()
      onClose()
    } catch (error) {
      const message = getFieldErrors(error).newPassword ?? getErrorMessage(error)
      setError('newPassword', { type: 'server', message })
    }
  })

  return (
    <Modal
      open
      onClose={onClose}
      title="Restablecer contraseña"
      description={`${user.fullName} (${user.username})`}
      size="sm"
      dismissible={!isSubmitting}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" form="reset-password-form" loading={isSubmitting}>
            Restablecer
          </Button>
        </>
      }
    >
      <form id="reset-password-form" onSubmit={submit} noValidate className="space-y-4">
        <p className="text-sm text-slate-600">
          Se cerrarán las sesiones abiertas del usuario y su cuenta quedará desbloqueada. La contraseña es
          temporal: deberá cambiarla al iniciar sesión.
        </p>
        <PasswordField label="Nueva contraseña" registration={register('newPassword')} error={errors.newPassword} hint={PASSWORD_HINT} autoFocus />
        <PasswordField label="Confirmar contraseña" registration={register('confirmPassword')} error={errors.confirmPassword} />
      </form>
    </Modal>
  )
}
