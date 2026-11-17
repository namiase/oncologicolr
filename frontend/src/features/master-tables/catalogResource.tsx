import { z } from 'zod'
import type { CrudEntity, CrudResource, FieldConfig } from '@/components/crud'
import { StatusBadge } from '@/components/ui/Badge'
import { ACTIVE_STATUS_ID } from '@/types/api'

export interface CatalogDefinition {
  key: string
  name: string
  singularName: string
  group: string
  parentKey: string | null
  parentSingularName: string | null
  feminine: boolean
}

export interface CatalogItem extends CrudEntity {
  code: string | null
  name: string
  description: string | null
  sortOrder: number
  statusId: string
  statusName: string
  parentId: string | null
  parentName: string | null
  createdAt: string
  createdBy: string
  updatedAt: string | null
  updatedBy: string | null
}

export interface CatalogForm {
  code: string
  name: string
  description: string
  sortOrder: number
  statusId: string
  parentId: string
}

/**
 * Construye la configuración del CRUD genérico para cualquier tabla maestra a partir
 * de su definición (GET /api/catalogs). Si el catálogo tiene padre (Estado → País),
 * agrega automáticamente la columna, el campo y el filtro del padre.
 */
export function buildCatalogResource(definition: CatalogDefinition): CrudResource<CatalogItem, CatalogForm> {
  const parentLabel = definition.parentSingularName ?? 'Padre'
  const parentLookup = definition.parentKey ? `/catalogs/${definition.parentKey}/lookup` : undefined

  const schema = z.object({
    code: z.string().trim().max(20, 'Máximo 20 caracteres.'),
    name: z.string().trim().min(1, 'El nombre es obligatorio.').max(150, 'Máximo 150 caracteres.'),
    description: z.string().trim().max(500, 'Máximo 500 caracteres.'),
    sortOrder: z
      .number({ error: 'Ingrese un número.' })
      .int('Debe ser un número entero.')
      .min(0, 'Mínimo 0.')
      .max(9999, 'Máximo 9999.'),
    statusId: z.string().min(1, 'Seleccione el estatus.'),
    parentId: parentLookup ? z.string().min(1, `Seleccione el ${parentLabel.toLowerCase()}.`) : z.string(),
  })

  const parentField: FieldConfig<CatalogForm, CatalogItem>[] = parentLookup
    ? [
        {
          name: 'parentId',
          label: parentLabel,
          type: 'lookup',
          required: true,
          lookupPath: parentLookup,
          currentOption: (item) => (item.parentId ? { value: item.parentId, label: item.parentName ?? '' } : null),
        },
      ]
    : []

  return {
    title: definition.name,
    singular: definition.singularName,
    feminine: definition.feminine,
    description: `Tabla maestra · ${definition.group}`,
    basePath: `/catalogs/${definition.key}`,
    columns: [
      { key: 'code', header: 'Código', sortKey: 'code', hideOnMobile: true, className: 'w-28 font-mono text-xs' },
      {
        key: 'name',
        header: 'Nombre',
        sortKey: 'name',
        render: (item) => (
          <div className="sm:min-w-40">
            <p className="font-medium text-slate-900">{item.name}</p>
            {item.description && <p className="mt-0.5 line-clamp-1 text-xs text-slate-500">{item.description}</p>}
          </div>
        ),
      },
      ...(parentLookup ? [{ key: 'parentName', header: parentLabel, sortKey: 'parent' }] : []),
      { key: 'sortOrder', header: 'Orden', sortKey: 'sortOrder', hideOnMobile: true, className: 'w-20 tabular-nums' },
      {
        key: 'status',
        header: 'Estatus',
        sortKey: 'status',
        render: (item) => <StatusBadge statusId={item.statusId} statusName={item.statusName} />,
      },
    ],
    fields: [
      { name: 'name', label: 'Nombre', type: 'text', required: true, maxLength: 150, fullWidth: !parentLookup },
      ...parentField,
      { name: 'code', label: 'Código', type: 'text', maxLength: 20, hint: 'Opcional. Debe ser único.' },
      { name: 'sortOrder', label: 'Orden', type: 'number', hint: 'Posición en las listas desplegables.' },
      { name: 'statusId', label: 'Estatus', type: 'status', required: true },
      { name: 'description', label: 'Descripción', type: 'textarea', maxLength: 500, fullWidth: true },
    ],
    schema,
    emptyForm: { code: '', name: '', description: '', sortOrder: 0, statusId: ACTIVE_STATUS_ID, parentId: '' },
    toForm: (item) => ({
      code: item.code ?? '',
      name: item.name,
      description: item.description ?? '',
      sortOrder: item.sortOrder,
      statusId: item.statusId,
      parentId: item.parentId ?? '',
    }),
    toPayload: (form) => ({
      ...form,
      code: form.code || null,
      description: form.description || null,
      parentId: form.parentId || null,
    }),
    getLabel: (item) => item.name,
    filters: {
      status: true,
      parent: parentLookup ? { label: parentLabel, lookupPath: parentLookup } : undefined,
    },
  }
}
