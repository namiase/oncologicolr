import { useMemo, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { CrudManager } from '@/components/crud'
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
  const currentUserId = useAuthStore((s) => s.user?.id)
  const queryClient = useQueryClient()
  const [resetTarget, setResetTarget] = useState<UserItem | null>(null)

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

  const resource = useMemo(
    () =>
      buildUsersResource({
        currentUserId,
        canUpdate: permissions.canUpdate,
        onResetPassword: setResetTarget,
        onUnlock: (user) => unlockUser(user),
      }),
    [currentUserId, permissions.canUpdate, unlockUser],
  )

  return (
    <>
      <PageHeader title="Usuarios" description="Cree las cuentas de acceso, asigne su perfil y vincúlelas con una trabajadora social." />
      <CrudManager resource={resource} permissions={permissions} />
      {resetTarget && (
        <ResetPasswordDialog user={resetTarget} onClose={() => setResetTarget(null)} onReset={refreshUsers} />
      )}
    </>
  )
}
