import { useState } from 'react'
import { downloadReportPdf, useReportContext } from './reportsApi'
import { SignerDialog, type DownloadTarget } from './SignerDialog'

/**
 * Descarga del PDF. Una trabajadora social lo descarga directo (firma ella); un administrador
 * primero elige quién firma. Devuelve la acción, el diálogo a dibujar y qué informe se descarga.
 */
export function useReportDownload() {
  const context = useReportContext()
  const [choosing, setChoosing] = useState<DownloadTarget | null>(null)
  const [downloadingId, setDownloadingId] = useState<string | null>(null)

  const download = async (report: DownloadTarget, failurePrefix?: string) => {
    if (context.data?.canChooseSocialWorker) {
      setChoosing(report)
      return
    }
    setDownloadingId(report.id)
    await downloadReportPdf(report.id, undefined, failurePrefix)
    setDownloadingId(null)
  }

  const dialog = choosing ? <SignerDialog report={choosing} onClose={() => setChoosing(null)} /> : null

  return { download, dialog, downloadingId }
}
