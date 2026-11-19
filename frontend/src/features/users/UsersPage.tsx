import { useMemo, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { CrudManager } from '@/components/crud'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { PageHeader } from '@/components/ui/Feedback'
import { usePermissions } from '@/features/auth/usePermissions'
import { getErrorMessage } from '@/lib/errors'
import { useAuthStore } from '@/stores/authStore'
import { MenuCodes } from '@/types/api'
import { ResetPasswordDialog } from './ResetPasswordDialog'
import { usersApi } from './usersApi'
import { buildUsersResource, USERS_BASE_PATH, type UserItem } from './usersResource'

export function UsersPage() {
  const permissions = usePermissions(MenuCodes.Users)
  const currentUser = useAuthStore((s) => s.user)
  const queryClient = useQueryClient()
  const [resetTarget, setResetTarget] = useState<UserItem | null>(null)
  const [delegationTarget, setDelegationTarget] = useState<UserItem | null>(null)

  // Misma clave que usa CrudManager para la lista (['crud', basePath, params]).
  const refreshUsers = () => void queryClient.invalidateQueries({ queryKey: ['crud', USERS_BASE_PATH] })

  const unlock = useMutation({
    mutationFn: (user: UserItem) => usersApi.unlock(user.id),
    onSuccess: (_, user) => {
      toast.success(`Se desbloqueó la cuenta de ${user.username}.`)
      refreshUsers()
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })
  const unlockUser = unlock.mutate

  const delegation = useMutation({
    mutationFn: (user: UserItem) => usersApi.setAdminDelegation(user.id, !user.canManageAdministrators),
    onSuccess: (_, user) => {
      toast.success(
        user.canManageAdministrators
          ? `${user.username} ya no puede crear administradores.`
          : `${user.username} ahora puede crear administradores.`,
      )
      setDelegationTarget(null)
      refreshUsers()
    },
    onError: (error) => {
      toast.error(getErrorMessage(error))
      setDelegationTarget(null)
    },
  })

  const resource = useMemo(
    () =>
      buildUsersResource({
        currentUser,
        canUpdate: permissions.canUpdate,
        onResetPassword: setResetTarget,
        onUnlock: (user) => unlockUser(user),
        onToggleDelegation: setDelegationTarget,
      }),
    [currentUser, permissions.canUpdate, unlockUser],
  )

  const revoking = delegationTarget?.canManageAdministrators ?? false

  return (
    <>
      <PageHeader title="Usuarios" description="Cree las cuentas de acceso, asigne su perfil y vincúlelas con una trabajadora social." />
      <CrudManager resource={resource} permissions={permissions} showHeading={false} />
      {resetTarget && (
        <ResetPasswordDialog user={resetTarget} onClose={() => setResetTarget(null)} onReset={refreshUsers} />
      )}
      <ConfirmDialog
        open={delegationTarget !== null}
        title={revoking ? 'Quitar autorización' : 'Autorizar para crear administradores'}
        message={
          delegationTarget
            ? revoking
              ? `${delegationTarget.username} dejará de poder crear administradores y de gestionar sus cuentas. Su sesión se cerrará para aplicar el cambio.`
              : `${delegationTarget.username} podrá crear administradores y modificar, restablecer la contraseña o eliminar a los administradores sin esta autorización. Su sesión se cerrará para aplicar el cambio.`
            : ''
        }
        confirmLabel={revoking ? 'Quitar autorización' : 'Autorizar'}
        loading={delegation.isPending}
        onConfirm={() => delegationTarget && delegation.mutate(delegationTarget)}
        onClose={() => setDelegationTarget(null)}
      />
    </>
  )
}
