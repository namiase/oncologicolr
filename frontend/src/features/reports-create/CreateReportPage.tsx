import { FilePlus } from 'lucide-react'
import { Card, EmptyState, PageHeader } from '@/components/ui/Feedback'

/** Vista reservada: se construirá a partir de los mockups del informe social. */
export function CreateReportPage() {
  return (
    <>
      <PageHeader title="Crear informe" description="Elaboración de un nuevo informe social." />
      <Card>
        <EmptyState
          icon={FilePlus}
          title="Próximamente"
          description="Aquí podrá elaborar el informe social de cada paciente y su grupo familiar."
        />
      </Card>
    </>
  )
}
