import { useState } from 'react'
import { ChartColumn, FileText } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Card, EmptyState, PageHeader } from '@/components/ui/Feedback'
import { Field } from '@/components/ui/Field'
import { Input } from '@/components/ui/Input'
import { SearchableSelect } from '@/components/ui/SearchableSelect'
import { useLookup } from '@/components/crud/useCrudResource'

/** Vista inicial de reportes: filtros listos, a la espera del módulo de informes. */
export function ReportsPage() {
  const socialWorkers = useLookup('/social-workers/lookup')
  const [workerId, setWorkerId] = useState('')

  return (
    <>
      <PageHeader title="Reportes" description="Generación y consulta de reportes del sistema." />
      <Card className="mb-6 p-4">
        <form
          className="grid gap-4 sm:grid-cols-2 lg:grid-cols-[1fr_1fr_1.5fr_auto] lg:items-end"
          onSubmit={(e) => e.preventDefault()}
        >
          <Field id="report-from" label="Desde">
            <Input id="report-from" type="date" />
          </Field>
          <Field id="report-to" label="Hasta">
            <Input id="report-to" type="date" />
          </Field>
          <Field id="report-worker" label="Trabajadora social">
            <SearchableSelect
              id="report-worker"
              value={workerId}
              onChange={setWorkerId}
              placeholder="Todas"
              emptyLabel="Todas"
              loading={socialWorkers.isPending}
              options={(socialWorkers.data ?? []).map((w) => ({ value: w.id, label: w.name }))}
            />
          </Field>
          <Button type="submit" icon={<ChartColumn className="size-4" />} disabled title="Disponible con el módulo de informes">
            Generar
          </Button>
        </form>
      </Card>
      <Card>
        <EmptyState
          icon={FileText}
          title="Aún no hay informes para reportar"
          description="Los reportes estarán disponibles cuando se registren informes sociales en el sistema."
        />
      </Card>
    </>
  )
}
