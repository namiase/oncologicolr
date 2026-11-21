import type { ReactNode } from 'react'
import { CircleCheck, FileDown } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Feedback'
import type { SocialReport } from './reportsApi'

interface SavedReportPanelProps {
  report: SocialReport
  title: string
  actions: ReactNode
  onDownload: () => void
  downloading: boolean
}

/** Confirmación tras guardar: el PDF ya se descargó (o se está eligiendo quién firma); se puede volver a descargar. */
export function SavedReportPanel({ report, title, actions, onDownload, downloading }: SavedReportPanelProps) {
  return (
    <Card className="px-6 py-10 sm:px-10">
      <div className="mx-auto flex max-w-xl flex-col items-center text-center" role="status">
        <span className="flex size-12 items-center justify-center rounded-full bg-emerald-50 text-emerald-600 dark:bg-emerald-500/12 dark:text-emerald-300">
          <CircleCheck className="size-6" aria-hidden />
        </span>
        <h2 className="mt-4 text-lg font-bold text-slate-900">{title}</h2>
        <p className="mt-1 text-sm text-slate-600">
          {report.lastNames}, {report.firstNames} · Historia clínica {report.medicalRecordNumber}
        </p>
        <p className="mt-3 text-sm text-slate-500">
          El PDF se descarga en su equipo, listo para imprimir en 2 hojas tamaño Carta. Si no lo ve, descárguelo de nuevo.
        </p>
        <div className="mt-6 flex flex-wrap justify-center gap-2">
          <Button variant="secondary" icon={<FileDown className="size-4" />} loading={downloading} onClick={onDownload}>
            Descargar PDF de nuevo
          </Button>
          {actions}
        </div>
      </div>
    </Card>
  )
}
