import { useState } from 'react'
import type { FieldValues } from 'react-hook-form'
import { Plus, RefreshCw, Search, X } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/Button'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { Card, EmptyState } from '@/components/ui/Feedback'
import { Input, Select } from '@/components/ui/Input'
import { Pagination } from '@/components/ui/Pagination'
import { getErrorMessage } from '@/lib/errors'
import { formatNumber } from '@/lib/format'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { CrudFormModal } from './CrudFormModal'
import { CrudTable } from './CrudTable'
import { crudTexts, type CrudEntity, type CrudListParams, type CrudPermissions, type CrudResource } from './types'
import { useCrudList, useCrudMutations, useLookup, useStatuses } from './useCrudResource'

interface CrudManagerProps<TItem extends CrudEntity, TForm extends FieldValues> {
  resource: CrudResource<TItem, TForm>
  permissions: CrudPermissions
  /**
   * Muestra el título y la descripción del recurso en la tarjeta. Apáguelo cuando la página
   * ya tiene su propio encabezado con el mismo nombre (evita repetir "Usuarios / Usuarios").
   */
  showHeading?: boolean
}

type Editing<TItem> = { item: TItem | null } | null

const recordCount = (n: number) => `${formatNumber(n)} ${n === 1 ? 'registro' : 'registros'}`

/**
 * Pantalla CRUD genérica: búsqueda, filtros, tabla ordenable, paginación,
 * alta/edición en modal y eliminación lógica con confirmación.
 * Para reiniciar su estado al cambiar de recurso, renderizar con key={resource.basePath}.
 */
export function CrudManager<TItem extends CrudEntity, TForm extends FieldValues>({
  resource,
  permissions,
  showHeading = true,
}: CrudManagerProps<TItem, TForm>) {
  const [search, setSearch] = useState('')
  const [statusId, setStatusId] = useState('')
  const [parentId, setParentId] = useState('')
  const [pageSize, setPageSize] = useState(10)
  const [sort, setSort] = useState({ sortBy: '', sortDesc: false })
  const [editing, setEditing] = useState<Editing<TItem>>(null)
  const [deleting, setDeleting] = useState<TItem | null>(null)

  const debouncedSearch = useDebouncedValue(search.trim(), 300)

  // La página vuelve a 1 cuando cambia cualquier filtro (estado derivado, sin efectos).
  const filtersKey = JSON.stringify([debouncedSearch, statusId, parentId, pageSize, sort])
  const [pageState, setPageState] = useState({ key: filtersKey, page: 1 })
  const page = pageState.key === filtersKey ? pageState.page : 1
  const setPage = (next: number) => setPageState({ key: filtersKey, page: next })

  const parentParam = resource.filters?.parent?.param ?? 'parentId'
  const params: CrudListParams = {
    page,
    pageSize,
    search: debouncedSearch,
    statusId,
    ...sort,
    extra: parentId ? { [parentParam]: parentId } : {},
  }
  const list = useCrudList<TItem>(resource.basePath, params)
  const { create, update, remove } = useCrudMutations<TItem>(resource.basePath)
  const statuses = useStatuses()
  const parents = useLookup(resource.filters?.parent?.lookupPath)

  const items = list.data?.items ?? []
  const hasFilters = Boolean(debouncedSearch || statusId || parentId)
  const toPayload = resource.toPayload ?? ((form: TForm) => form)
  const texts = crudTexts(resource.singular, resource.feminine)

  const handleSort = (sortKey: string) =>
    setSort((current) =>
      current.sortBy === sortKey ? { sortBy: sortKey, sortDesc: !current.sortDesc } : { sortBy: sortKey, sortDesc: false },
    )

  const handleSubmit = async (form: TForm) => {
    const target = editing?.item
    if (target) {
      await update.mutateAsync({ id: target.id, payload: toPayload(form, 'edit') })
      toast.success(texts.updated)
    } else {
      await create.mutateAsync(toPayload(form, 'create'))
      toast.success(texts.created)
    }
    setEditing(null)
  }

  const handleDelete = async () => {
    if (!deleting) return
    try {
      await remove.mutateAsync(deleting.id)
      toast.success(`Se eliminó "${resource.getLabel(deleting)}".`)
      if (items.length === 1 && page > 1) setPage(page - 1)
      setDeleting(null)
    } catch (error) {
      toast.error(getErrorMessage(error))
      setDeleting(null)
    }
  }

  const clearFilters = () => {
    setSearch('')
    setStatusId('')
    setParentId('')
  }

  return (
    <Card>
      <div className="flex flex-col gap-4 border-b border-slate-200 p-4 sm:flex-row sm:items-start sm:justify-between">
        {showHeading ? (
          <div>
            <h2 className="text-lg font-semibold text-slate-900">{resource.title}</h2>
            <p className="mt-0.5 text-sm text-slate-500">
              {resource.description ?? 'Administre los registros de esta tabla.'}
              {list.data && <span className="ml-1">· {recordCount(list.data.totalCount)}</span>}
            </p>
          </div>
        ) : (
          <p className="text-sm text-slate-500 sm:self-center">
            {list.data ? recordCount(list.data.totalCount) : ' '}
          </p>
        )}
        {permissions.canCreate && (
          <Button icon={<Plus className="size-4" />} onClick={() => setEditing({ item: null })}>
            {texts.newLabel}
          </Button>
        )}
      </div>

      <div className="flex flex-col gap-3 border-b border-slate-200 p-4 md:flex-row md:items-center">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" aria-hidden />
          <Input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={resource.searchPlaceholder ?? 'Buscar por nombre, código o descripción…'}
            aria-label={`Buscar en ${resource.title}`}
            className="pl-9"
          />
        </div>
        {resource.filters?.parent && (
          <Select
            value={parentId}
            onChange={(e) => setParentId(e.target.value)}
            aria-label={`Filtrar por ${resource.filters.parent.label}`}
            placeholder={`${resource.filters.parent.label}: todos`}
            options={(parents.data ?? []).map((p) => ({ value: p.id, label: p.name, group: p.parentName ?? undefined }))}
            className="md:w-56"
          />
        )}
        {resource.filters?.status !== false && (
          <Select
            value={statusId}
            onChange={(e) => setStatusId(e.target.value)}
            aria-label="Filtrar por estatus"
            placeholder="Estatus: todos"
            options={(statuses.data ?? []).map((s) => ({ value: s.id, label: s.name }))}
            className="md:w-44"
          />
        )}
        {hasFilters && (
          <Button variant="ghost" icon={<X className="size-4" />} onClick={clearFilters}>
            Limpiar
          </Button>
        )}
      </div>

      {list.isError ? (
        <EmptyState
          title="No se pudieron cargar los registros"
          description={getErrorMessage(list.error)}
          action={<Button variant="secondary" icon={<RefreshCw className="size-4" />} onClick={() => list.refetch()}>Reintentar</Button>}
        />
      ) : !list.isPending && items.length === 0 ? (
        <EmptyState
          title={hasFilters ? 'Sin resultados' : 'Aún no hay registros'}
          description={hasFilters ? 'Ningún registro coincide con los filtros aplicados.' : `Cree el primer registro de ${resource.title.toLowerCase()}.`}
          action={
            hasFilters ? (
              <Button variant="secondary" onClick={clearFilters}>Limpiar filtros</Button>
            ) : (
              permissions.canCreate && (
                <Button icon={<Plus className="size-4" />} onClick={() => setEditing({ item: null })}>
                  {texts.newLabel}
                </Button>
              )
            )
          }
        />
      ) : (
        <>
          <CrudTable
            columns={resource.columns}
            items={items}
            loading={list.isPending}
            refreshing={list.isPlaceholderData}
            sortBy={sort.sortBy}
            sortDesc={sort.sortDesc}
            onSort={handleSort}
            permissions={permissions}
            onEdit={(item) => setEditing({ item })}
            onDelete={setDeleting}
            getLabel={resource.getLabel}
            rowActions={resource.rowActions}
            canEditItem={resource.canEditItem}
            canDeleteItem={resource.canDeleteItem}
          />
          {list.data && (
            <Pagination
              page={page}
              pageSize={pageSize}
              totalCount={list.data.totalCount}
              totalPages={list.data.totalPages}
              onPageChange={setPage}
              onPageSizeChange={setPageSize}
            />
          )}
        </>
      )}

      {editing && (
        <CrudFormModal
          key={editing.item?.id ?? 'new'}
          resource={resource}
          item={editing.item}
          onClose={() => setEditing(null)}
          onSubmit={handleSubmit}
        />
      )}

      <ConfirmDialog
        open={deleting !== null}
        title={texts.deleteLabel}
        message={deleting ? `¿Desea eliminar "${resource.getLabel(deleting)}"? El registro quedará inactivo y dejará de aparecer en las listas.` : ''}
        loading={remove.isPending}
        onConfirm={handleDelete}
        onClose={() => setDeleting(null)}
      />
    </Card>
  )
}
