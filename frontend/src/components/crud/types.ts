import type { ReactNode } from 'react'
import type { FieldValues, Path } from 'react-hook-form'
import type { ZodType } from 'zod'

/** Mínimo que debe tener cualquier registro administrable con el CRUD genérico. */
export interface CrudEntity {
  id: string
  statusId?: string
  statusName?: string
}

export interface CrudPermissions {
  canCreate: boolean
  canUpdate: boolean
  canDelete: boolean
}

export interface ColumnConfig<TItem> {
  key: string
  header: string
  /** Clave de ordenamiento que entiende la API (?sortBy=). Sin ella, la columna no es ordenable. */
  sortKey?: string
  render?: (item: TItem) => ReactNode
  className?: string
  hideOnMobile?: boolean
}

export type FieldType = 'text' | 'password' | 'textarea' | 'number' | 'select' | 'lookup' | 'status'

export type FormMode = 'create' | 'edit'

export interface FieldOption {
  value: string
  label: string
}

export interface FieldConfig<TForm extends FieldValues, TItem = unknown> {
  name: Path<TForm>
  label: string
  type: FieldType
  required?: boolean
  placeholder?: string
  hint?: string
  maxLength?: number
  /** Ocupa las dos columnas del formulario. */
  fullWidth?: boolean
  /** Opciones fijas (type: 'select'). */
  options?: FieldOption[]
  /** Endpoint de opciones dinámicas (type: 'lookup'), p. ej. '/catalogs/countries/lookup'. */
  lookupPath?: string
  /** Opción actual del registro en edición, por si ya no está activa en el lookup. */
  currentOption?: (item: TItem) => FieldOption | null
  /** Mostrar el campo solo al crear o solo al editar (p. ej. la contraseña inicial). */
  only?: FormMode
  /** Visible pero no editable al editar (p. ej. el nombre de usuario). */
  readOnlyOnEdit?: boolean
  autoComplete?: string
}

export interface CrudFilters {
  status?: boolean
  /** Filtro por una lista (p. ej. país, perfil). param: nombre del parámetro en la API (por defecto parentId). */
  parent?: { label: string; lookupPath: string; param?: string }
}

/** Acción adicional por fila (además de Editar y Eliminar). */
export interface RowAction {
  key: string
  label: string
  icon: ReactNode
  onClick: () => void
}

/**
 * Describe un recurso administrable. El mismo componente <CrudManager> sirve para
 * cualquier tabla maestra o entidad que siga el contrato REST:
 * GET {basePath}?search&statusId&parentId&page&pageSize&sortBy&sortDesc → PagedResult
 * POST {basePath} · PUT {basePath}/{id} · DELETE {basePath}/{id}
 */
export interface CrudResource<TItem extends CrudEntity, TForm extends FieldValues> {
  title: string
  /** Nombre en singular, p. ej. "Estado" (para "Nuevo estado"). */
  singular: string
  /** Género gramatical de singular: "Nueva raza", "Raza creada". */
  feminine?: boolean
  description?: string
  basePath: string
  columns: ColumnConfig<TItem>[]
  fields: FieldConfig<TForm, TItem>[]
  /** Validación al editar (y al crear si no hay createSchema). */
  schema: ZodType<TForm, TForm>
  /** Validación al crear, cuando difiere de la de edición (p. ej. exige contraseña). */
  createSchema?: ZodType<TForm, TForm>
  emptyForm: TForm
  toForm: (item: TItem) => TForm
  /** Convierte el formulario al cuerpo que espera la API (p. ej. '' → null). */
  toPayload?: (form: TForm, mode: FormMode) => unknown
  /** Texto con el que se identifica el registro en la confirmación de borrado. */
  getLabel: (item: TItem) => string
  filters?: CrudFilters
  searchPlaceholder?: string
  /** Acciones adicionales por fila. */
  rowActions?: (item: TItem) => RowAction[]
  /** Permite ocultar "Eliminar" en filas concretas (p. ej. el propio usuario). */
  canDeleteItem?: (item: TItem) => boolean
}

export interface CrudListParams {
  page: number
  pageSize: number
  search: string
  statusId: string
  sortBy: string
  sortDesc: boolean
  /** Filtros adicionales con su nombre de parámetro (p. ej. { parentId }, { roleId }). */
  extra: Record<string, string>
}

/** Textos con concordancia de género para un recurso. */
export const crudTexts = (singular: string, feminine = false) => {
  const lower = singular.toLowerCase()
  return {
    newLabel: `${feminine ? 'Nueva' : 'Nuevo'} ${lower}`,
    editLabel: `Editar ${lower}`,
    deleteLabel: `Eliminar ${lower}`,
    created: `${singular} ${feminine ? 'creada' : 'creado'} correctamente.`,
    updated: `${singular} ${feminine ? 'actualizada' : 'actualizado'} correctamente.`,
  }
}
