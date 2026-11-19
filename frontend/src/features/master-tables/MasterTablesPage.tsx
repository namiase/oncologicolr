import { useMemo } from 'react'
import { Navigate, NavLink, useNavigate, useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { Database, RefreshCw } from 'lucide-react'
import { CrudManager } from '@/components/crud'
import { Button } from '@/components/ui/Button'
import { Card, EmptyState, PageHeader, Spinner } from '@/components/ui/Feedback'
import { Select } from '@/components/ui/Input'
import { usePermissions } from '@/features/auth/usePermissions'
import { api } from '@/lib/api'
import { cn } from '@/lib/cn'
import { getErrorMessage } from '@/lib/errors'
import { MenuCodes } from '@/types/api'
import { buildCatalogResource, type CatalogDefinition } from './catalogResource'

const BASE = '/configuracion/tablas-maestras'

/** Administración de todas las tablas maestras con un único componente CRUD genérico. */
export function MasterTablesPage() {
  const { catalogKey } = useParams()
  const navigate = useNavigate()
  const permissions = usePermissions(MenuCodes.MasterTables)

  const definitions = useQuery({
    queryKey: ['catalog-definitions'],
    queryFn: async () => (await api.get<CatalogDefinition[]>('/catalogs')).data,
    staleTime: Infinity,
  })

  const groups = useMemo(() => {
    const map = new Map<string, CatalogDefinition[]>()
    for (const d of definitions.data ?? []) map.set(d.group, [...(map.get(d.group) ?? []), d])
    return [...map.entries()]
  }, [definitions.data])

  const current = definitions.data?.find((d) => d.key === catalogKey)
  const resource = useMemo(() => (current ? buildCatalogResource(current) : null), [current])

  if (definitions.isPending) return <Spinner className="p-8" />

  if (definitions.isError) {
    return (
      <EmptyState
        titleAs="h1"
        title="No se pudieron cargar las tablas maestras"
        description={getErrorMessage(definitions.error)}
        action={
          <Button variant="secondary" icon={<RefreshCw className="size-4" />} onClick={() => definitions.refetch()}>
            Reintentar
          </Button>
        }
      />
    )
  }

  const first = definitions.data[0]
  if (!catalogKey && first) return <Navigate to={`${BASE}/${first.key}`} replace />

  return (
    <>
      <PageHeader title="Tablas maestras" description="Catálogos generales que alimentan los formularios del sistema." />
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[15rem_minmax(0,1fr)]">
        {/* Selector en móvil */}
        <Select
          className="lg:hidden"
          aria-label="Tabla maestra"
          value={catalogKey ?? ''}
          onChange={(e) => navigate(`${BASE}/${e.target.value}`)}
          options={definitions.data.map((d) => ({ value: d.key, label: `${d.group} · ${d.name}` }))}
        />

        {/* Lista agrupada en escritorio */}
        <nav aria-label="Tablas maestras" className="hidden lg:block">
          <Card className="sticky top-24 p-2">
            {groups.map(([group, items]) => (
              <div key={group} className="py-1">
                <p className="px-3 pt-2 pb-1 text-xs font-semibold tracking-wide text-slate-500 uppercase">{group}</p>
                <ul>
                  {items.map((d) => (
                    <li key={d.key}>
                      <NavLink
                        to={`${BASE}/${d.key}`}
                        className={({ isActive }) =>
                          cn(
                            'block rounded-md px-3 py-1.5 text-sm',
                            isActive ? 'bg-brand-50 font-medium text-brand-800 dark:bg-brand-500/15 dark:text-brand-300' : 'text-slate-600 hover:bg-slate-100',
                          )
                        }
                      >
                        {d.name}
                      </NavLink>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </Card>
        </nav>

        <div className="min-w-0">
          {resource ? (
            <CrudManager key={resource.basePath} resource={resource} permissions={permissions} />
          ) : (
            <Card>
              <EmptyState icon={Database} title="Tabla no encontrada" description="Seleccione una tabla maestra de la lista." />
            </Card>
          )}
        </div>
      </div>
    </>
  )
}
