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
          title="Formulario del informe en diseño"
          description="Esta pantalla se construirá a partir de los mockups del informe. Ya cuenta con los catálogos (sexo, estado civil, parentesco, ubicación…) y las trabajadoras sociales para alimentar sus campos."
        />
      </Card>
    </>
  )
}
