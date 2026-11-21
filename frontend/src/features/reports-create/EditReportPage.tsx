import { useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import { ArrowLeft, FileX, Lock, Pencil } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/Button'
import { Card, EmptyState, PageHeader, Spinner } from '@/components/ui/Feedback'
import { usePermissions } from '@/features/auth/usePermissions'
import { getErrorMessage } from '@/lib/errors'
import { MenuCodes } from '@/types/api'
import { ReportForm } from './ReportForm'
import { reportsApi, useReportContext, type SocialReport } from './reportsApi'
import { SavedReportPanel } from './SavedReportPanel'
import { useReportDownload } from './useReportDownload'

const LIST_PATH = '/informes/nuevo?vista=informes'

/** Corrige un informe guardado y vuelve a descargar su PDF. */
export function EditReportPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { canUpdate } = usePermissions(MenuCodes.CreateReport)
  const context = useReportContext()
  const report = useQuery({
    queryKey: ['social-reports', 'detail', id],
    queryFn: () => reportsApi.get(id),
    enabled: canUpdate && Boolean(id),
    retry: (count, error) => !(isAxiosError(error) && error.response?.status === 404) && count < 2,
  })
  const [saved, setSaved] = useState<SocialReport | null>(null)
  const pdf = useReportDownload()

  const back = (
    <Button variant="secondary" icon={<ArrowLeft className="size-4" />} onClick={() => navigate(LIST_PATH)}>
      Volver a los informes
    </Button>
  )

  if (!canUpdate) {
    return (
      <Card>
        <EmptyState icon={Lock} title="Sin permiso para editar" description="Su perfil no permite corregir informes." action={back} />
      </Card>
    )
  }

  if (report.isPending || context.isPending) return <Spinner className="p-8" />

  if (report.isError || context.isError) {
    const notFound = isAxiosError(report.error) && report.error.response?.status === 404
    return (
      <Card>
        <EmptyState
          icon={FileX}
          title={notFound ? 'Informe no encontrado' : 'No se pudo cargar el informe'}
          description={notFound ? 'El informe no existe o no tiene acceso a él.' : getErrorMessage(report.error ?? context.error)}
          action={back}
        />
      </Card>
    )
  }

  const onSaved = async (updated: SocialReport) => {
    queryClient.setQueryData(['social-reports', 'detail', id], updated)
    void queryClient.invalidateQueries({ queryKey: ['social-reports', 'list'] })
    setSaved(updated)
    window.scrollTo({ top: 0, behavior: 'smooth' })
    toast.success('Cambios guardados.')
    await pdf.download(updated, 'Los cambios se guardaron, pero no se pudo descargar el PDF.')
  }

  const current = report.data

  return (
    <>
      <PageHeader
        title="Editar informe"
        description={`${current.lastNames}, ${current.firstNames} · Historia clínica ${current.medicalRecordNumber}`}
        actions={back}
      />
      {saved ? (
        <SavedReportPanel
          report={saved}
          title="Cambios guardados"
          downloading={pdf.downloadingId === saved.id}
          onDownload={() => void pdf.download(saved)}
          actions={
            <Button icon={<Pencil className="size-4" />} onClick={() => setSaved(null)}>
              Seguir editando
            </Button>
          }
        />
      ) : (
        <ReportForm key={current.updatedAt ?? current.createdAt} mode="edit" context={context.data} initial={current} onSaved={onSaved} />
      )}
      {pdf.dialog}
    </>
  )
}
