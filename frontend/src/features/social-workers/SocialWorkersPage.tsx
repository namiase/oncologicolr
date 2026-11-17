import { CrudManager } from '@/components/crud'
import { PageHeader } from '@/components/ui/Feedback'
import { usePermissions } from '@/features/auth/usePermissions'
import { MenuCodes } from '@/types/api'
import { socialWorkerResource } from './socialWorkerResource'

export function SocialWorkersPage() {
  const permissions = usePermissions(MenuCodes.SocialWorkers)
  return (
    <>
      <PageHeader
        title="Trabajadoras sociales"
        description="Nombres, cédula, N° de registro ANTS, colegio y año de grado."
      />
      <CrudManager resource={socialWorkerResource} permissions={permissions} />
    </>
  )
}
