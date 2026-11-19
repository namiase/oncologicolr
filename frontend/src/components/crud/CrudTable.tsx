import { ArrowDown, ArrowUp, ArrowUpDown, Pencil, Trash } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { cn } from '@/lib/cn'
import type { ColumnConfig, CrudEntity, CrudPermissions, RowAction } from './types'

interface CrudTableProps<TItem extends CrudEntity> {
  columns: ColumnConfig<TItem>[]
  items: TItem[]
  loading: boolean
  refreshing: boolean
  sortBy: string
  sortDesc: boolean
  onSort: (sortKey: string) => void
  permissions: CrudPermissions
  onEdit: (item: TItem) => void
  onDelete: (item: TItem) => void
  getLabel: (item: TItem) => string
  rowActions?: (item: TItem) => RowAction[]
  canDeleteItem?: (item: TItem) => boolean
}

export function CrudTable<TItem extends CrudEntity>({
  columns,
  items,
  loading,
  refreshing,
  sortBy,
  sortDesc,
  onSort,
  permissions,
  onEdit,
  onDelete,
  getLabel,
  rowActions,
  canDeleteItem,
}: CrudTableProps<TItem>) {
  const hasActions = permissions.canUpdate || permissions.canDelete || Boolean(rowActions)

  return (
    <div className="relative overflow-x-auto">
      <table className={cn('min-w-full divide-y divide-slate-200 text-sm transition-opacity', refreshing && 'opacity-60')}>
        <thead className="bg-slate-50">
          <tr>
            {columns.map((column) => {
              const active = column.sortKey !== undefined && column.sortKey === sortBy
              return (
                <th
                  key={column.key}
                  scope="col"
                  aria-sort={active ? (sortDesc ? 'descending' : 'ascending') : undefined}
                  className={cn(
                    'px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500',
                    column.hideOnMobile && 'hidden md:table-cell',
                    column.className,
                  )}
                >
                  {column.sortKey ? (
                    <button
                      type="button"
                      onClick={() => onSort(column.sortKey!)}
                      className="group inline-flex items-center gap-1 uppercase hover:text-slate-900"
                    >
                      {column.header}
                      {active ? (
                        sortDesc ? <ArrowDown className="size-3.5" aria-hidden /> : <ArrowUp className="size-3.5" aria-hidden />
                      ) : (
                        <ArrowUpDown className="size-3.5 opacity-0 transition-opacity group-hover:opacity-60" aria-hidden />
                      )}
                    </button>
                  ) : (
                    column.header
                  )}
                </th>
              )
            })}
            {hasActions && (
              <th scope="col" className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500">
                <span className="sr-only">Acciones</span>
              </th>
            )}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 bg-surface">
          {loading
            ? Array.from({ length: 5 }, (_, row) => (
                <tr key={row}>
                  {columns.map((column) => (
                    <td key={column.key} className={cn('px-4 py-3.5', column.hideOnMobile && 'hidden md:table-cell')}>
                      <div className="h-4 w-full max-w-40 animate-pulse rounded bg-slate-100" />
                    </td>
                  ))}
                  {hasActions && <td />}
                </tr>
              ))
            : items.map((item) => (
                <tr key={item.id} className="hover:bg-hover/70">
                  {columns.map((column) => (
                    <td
                      key={column.key}
                      className={cn('px-4 py-3 text-slate-700', column.hideOnMobile && 'hidden md:table-cell', column.className)}
                    >
                      {column.render
                        ? column.render(item)
                        : String((item as Record<string, unknown>)[column.key] ?? '—')}
                    </td>
                  ))}
                  {hasActions && (
                    <td className="whitespace-nowrap px-4 py-2 text-right">
                      <div className="inline-flex gap-1">
                        {rowActions?.(item).map((action) => (
                          <Button
                            key={action.key}
                            variant="ghost"
                            size="icon"
                            onClick={action.onClick}
                            aria-label={`${action.label}: ${getLabel(item)}`}
                            title={action.label}
                          >
                            {action.icon}
                          </Button>
                        ))}
                        {permissions.canUpdate && (
                          <Button variant="ghost" size="icon" onClick={() => onEdit(item)} aria-label={`Editar ${getLabel(item)}`} title="Editar">
                            <Pencil className="size-4" />
                          </Button>
                        )}
                        {permissions.canDelete && (canDeleteItem?.(item) ?? true) && (
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => onDelete(item)}
                            aria-label={`Eliminar ${getLabel(item)}`}
                            title="Eliminar"
                            className="hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-500/10 dark:hover:text-red-400"
                          >
                            <Trash className="size-4" />
                          </Button>
                        )}
                      </div>
                    </td>
                  )}
                </tr>
              ))}
        </tbody>
      </table>
    </div>
  )
}
