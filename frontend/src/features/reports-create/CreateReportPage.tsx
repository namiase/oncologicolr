import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import { useQueryClient } from '@tanstack/react-query'
import { FilePlus, Pencil } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/Button'
import { PageHeader, Spinner } from '@/components/ui/Feedback'
import { usePermissions } from '@/features/auth/usePermissions'
import { cn } from '@/lib/cn'
import { getErrorMessage } from '@/lib/errors'
import { MenuCodes } from '@/types/api'
import { MyReportsList } from './MyReportsList'
import { ReportForm } from './ReportForm'
import { useReportContext, type SocialReport } from './reportsApi'
import { SavedReportPanel } from './SavedReportPanel'
import { useReportDownload } from './useReportDownload'

type Tab = 'form' | 'list'

/** Entrevista preliminar: se llena en pantalla y al guardar se descarga el PDF para imprimir. */
export function CreateReportPage() {
  const [params, setParams] = useSearchParams()
  const tab: Tab = params.get('vista') === 'informes' ? 'list' : 'form'
  const context = useReportContext()
  const { canUpdate } = usePermissions(MenuCodes.CreateReport)
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [saved, setSaved] = useState<SocialReport | null>(null)
  const [formKey, setFormKey] = useState(0)
  const pdf = useReportDownload()

  const selectTab = (next: Tab) => setParams(next === 'list' ? { vista: 'informes' } : {}, { replace: true })

  const onSaved = async (report: SocialReport) => {
    setSaved(report)
    window.scrollTo({ top: 0, behavior: 'smooth' })
    void queryClient.invalidateQueries({ queryKey: ['social-reports', 'list'] })
    void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    toast.success('Informe guardado.')
    // Trabajadora social: se descarga ya. Administrador: primero elige quién firma.
    await pdf.download(report, 'El informe se guardó, pero no se pudo descargar el PDF.')
  }

  const startNew = () => {
    setSaved(null)
    setFormKey((key) => key + 1)
  }

  return (
    <>
      <PageHeader
        title="Crear informe"
        description="Entrevista preliminar del paciente. Al guardar se descarga el PDF para imprimir (2 hojas tamaño Carta)."
      />

      <div role="tablist" aria-label="Vistas" className="mb-5 inline-flex rounded-xl bg-slate-100 p-1">
        <TabButton id="tab-form" panel="panel-form" selected={tab === 'form'} onSelect={() => selectTab('form')}>
          Nuevo informe
        </TabButton>
        <TabButton id="tab-list" panel="panel-list" selected={tab === 'list'} onSelect={() => selectTab('list')}>
          {context.data?.canChooseSocialWorker ? 'Informes registrados' : 'Mis informes'}
        </TabButton>
      </div>

      {tab === 'form' ? (
        <div role="tabpanel" id="panel-form" aria-labelledby="tab-form">
          {context.isPending ? (
            <Spinner className="p-8" />
          ) : context.isError ? (
            <p className="text-sm text-red-600 dark:text-red-400">{getErrorMessage(context.error)}</p>
          ) : saved ? (
            <SavedReportPanel
              report={saved}
              title="Informe guardado"
              downloading={pdf.downloadingId === saved.id}
              onDownload={() => void pdf.download(saved)}
              actions={
                <>
                  {canUpdate && (
                    <Button variant="secondary" icon={<Pencil className="size-4" />} onClick={() => navigate(`/informes/${saved.id}/editar`)}>
                      Editar informe
                    </Button>
                  )}
                  <Button icon={<FilePlus className="size-4" />} onClick={startNew}>
                    Crear otro informe
                  </Button>
                </>
              }
            />
          ) : (
            <ReportForm key={formKey} mode="create" context={context.data} onSaved={onSaved} />
          )}
        </div>
      ) : (
        <div role="tabpanel" id="panel-list" aria-labelledby="tab-list">
          <MyReportsList canUpdate={canUpdate} showAll={Boolean(context.data?.canChooseSocialWorker)} />
        </div>
      )}
      {pdf.dialog}
    </>
  )
}

function TabButton({
  id,
  panel,
  selected,
  onSelect,
  children,
}: {
  id: string
  panel: string
  selected: boolean
  onSelect: () => void
  children: string
}) {
  return (
    <button
      type="button"
      role="tab"
      id={id}
      aria-selected={selected}
      aria-controls={panel}
      onClick={onSelect}
      className={cn(
        'h-9 rounded-lg px-4 text-sm font-semibold transition-colors',
        selected ? 'bg-surface text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900',
      )}
    >
      {children}
    </button>
  )
}
