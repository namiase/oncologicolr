import { useState } from 'react'
import { FileDown } from 'lucide-react'
import { useLookup } from '@/components/crud/useCrudResource'
import { Button } from '@/components/ui/Button'
import { Field } from '@/components/ui/Field'
import { Modal } from '@/components/ui/Modal'
import { SearchableSelect } from '@/components/ui/SearchableSelect'
import { downloadReportPdf } from './reportsApi'

export interface DownloadTarget {
  id: string
  socialWorkerId: string | null
  medicalRecordNumber: string
  /** Lista de informes: "Apellidos, Nombres"; el informe completo trae los dos campos. */
  patientName?: string
  lastNames?: string
  firstNames?: string
}

const patientOf = (report: DownloadTarget) => report.patientName ?? [report.lastNames, report.firstNames].filter(Boolean).join(', ')

/**
 * Solo administradores: antes de descargar el PDF eligen quién firma. Por defecto, la trabajadora
 * del informe; sin elegir ninguna, la firma sale en blanco para llenarla a mano.
 */
export function SignerDialog({ report, onClose }: { report: DownloadTarget; onClose: () => void }) {
  const socialWorkers = useLookup('/social-workers/lookup')
  const [signerId, setSignerId] = useState(report.socialWorkerId ?? '')
  const [downloading, setDownloading] = useState(false)

  const download = async () => {
    setDownloading(true)
    const ok = await downloadReportPdf(report.id, signerId || undefined)
    setDownloading(false)
    if (ok) onClose()
  }

  const options = (socialWorkers.data ?? []).map((w) => ({ value: w.id, label: w.name, keywords: w.code ?? undefined }))

  return (
    <Modal
      open
      onClose={onClose}
      title="Descargar PDF"
      description={`${patientOf(report)} · Historia clínica ${report.medicalRecordNumber}. Elija la trabajadora social que firma el informe impreso.`}
      size="sm"
      dismissible={!downloading}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={downloading}>
            Cancelar
          </Button>
          <Button icon={<FileDown className="size-4" />} loading={downloading} onClick={() => void download()}>
            Descargar PDF
          </Button>
        </>
      }
    >
      <Field id="signer" label="Trabajador(a) social tratante" hint="Sin trabajadora, la firma sale en blanco para llenarla a mano.">
        <SearchableSelect
          id="signer"
          value={signerId}
          options={options}
          loading={socialWorkers.isPending}
          placeholder="Firma en blanco"
          emptyLabel="Firma en blanco"
          describedBy="signer-hint"
          onChange={setSignerId}
        />
      </Field>
    </Modal>
  )
}
