export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface LookupItem {
  id: string
  name: string
  code: string | null
  parentId: string | null
  /** Nombre del padre (p. ej. el país de un estado): distingue nombres repetidos. */
  parentName: string | null
}

/** Respuesta de error estándar de la API (RFC 9457). */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
  /** Campo afectado en errores 409 (duplicados). */
  field?: string
}

export interface MenuPermissions {
  canCreate: boolean
  canUpdate: boolean
  canDelete: boolean
}

export interface MenuItem {
  id: string
  code: string
  title: string
  path: string | null
  icon: string | null
  sortOrder: number
  permissions: MenuPermissions
  children: MenuItem[]
}

export interface CurrentUser {
  id: string
  username: string
  fullName: string
  email: string | null
  roleId: string
  roleName: string
  /** Tiene una contraseña asignada por un administrador: debe cambiarla antes de usar el sistema. */
  mustChangePassword: boolean
  /** Administrador principal (el que creó el sistema): nadie más puede modificar su cuenta. */
  isSuperAdmin: boolean
  /** Puede crear administradores y gestionar sus cuentas (el principal o un autorizado por él). */
  canManageAdministrators: boolean
  menus: MenuItem[]
}

export interface AuthResponse {
  accessToken: string
  expiresAt: string
  /** Vida del access token en segundos (relativa: no depende del reloj del equipo). */
  expiresInSeconds: number
  /** Minutos de inactividad tras los cuales se cierra la sesión. */
  idleTimeoutMinutes: number
  user: CurrentUser
}

/** Códigos de menú definidos en el backend (MenuCodes). */
export const MenuCodes = {
  Home: 'HOME',
  CreateReport: 'CREATE_REPORT',
  Configuration: 'CONFIGURATION',
  MasterTables: 'MASTER_TABLES',
  SocialWorkers: 'SOCIAL_WORKERS',
  Users: 'USERS',
  CompanySettings: 'COMPANY_SETTINGS',
  Reports: 'REPORTS',
} as const

export type MenuCode = (typeof MenuCodes)[keyof typeof MenuCodes]

/** Estatus fijos del backend (StatusIds). */
export const ACTIVE_STATUS_ID = '00000000-0000-0000-0000-000000000001'
