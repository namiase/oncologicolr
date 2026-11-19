import { KeyRound, LockOpen } from 'lucide-react'
import { z } from 'zod'
import type { CrudEntity, CrudResource, RowAction } from '@/components/crud'
import { Badge, StatusBadge } from '@/components/ui/Badge'
import { formatDateTime } from '@/lib/format'
import { ACTIVE_STATUS_ID } from '@/types/api'
import { containsUsername, PASSWORD_HINT, passwordRule, USERNAME_IN_PASSWORD } from './passwordRules'

export interface UserItem extends CrudEntity {
  username: string
  fullName: string
  email: string | null
  roleId: string
  roleName: string
  statusId: string
  statusName: string
  lastLoginAt: string | null
  isLockedOut: boolean
  mustChangePassword: boolean
  socialWorkerId: string | null
  socialWorkerName: string | null
}

export interface UserForm {
  username: string
  fullName: string
  email: string
  roleId: string
  statusId: string
  socialWorkerId: string
  password: string
  confirmPassword: string
}

export const USERS_BASE_PATH = '/users'

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const baseShape = {
  username: z.string(),
  fullName: z.string().trim().min(1, 'El nombre completo es obligatorio.').max(150, 'Máximo 150 caracteres.'),
  email: z.string().trim().max(150, 'Máximo 150 caracteres.').refine((v) => v === '' || EMAIL.test(v), 'Correo inválido.'),
  roleId: z.string().min(1, 'Seleccione el perfil.'),
  statusId: z.string().min(1, 'Seleccione el estatus.'),
  socialWorkerId: z.string(),
  password: z.string(),
  confirmPassword: z.string(),
}

const editSchema = z.object(baseShape)

const createSchema = z
  .object({
    ...baseShape,
    username: z
      .string()
      .trim()
      .regex(/^[a-zA-Z0-9._-]{3,50}$/, 'De 3 a 50 caracteres: letras, números, punto, guion o guion bajo (sin espacios).'),
    password: passwordRule,
  })
  .refine((v) => v.password === v.confirmPassword, { path: ['confirmPassword'], message: 'Las contraseñas no coinciden.' })
  .refine((v) => !containsUsername(v.password, v.username), { path: ['password'], message: USERNAME_IN_PASSWORD })

interface UsersResourceOptions {
  currentUserId: string | undefined
  canUpdate: boolean
  onResetPassword: (user: UserItem) => void
  onUnlock: (user: UserItem) => void
}

/** Usuarios del sistema con el mismo CRUD genérico de las tablas maestras. */
export function buildUsersResource({
  currentUserId,
  canUpdate,
  onResetPassword,
  onUnlock,
}: UsersResourceOptions): CrudResource<UserItem, UserForm> {
  return {
    title: 'Usuarios',
    singular: 'Usuario',
    description: 'Cuentas de acceso al sistema, su perfil y su estatus.',
    basePath: USERS_BASE_PATH,
    searchPlaceholder: 'Buscar por usuario, nombre o correo…',
    columns: [
      {
        key: 'username',
        header: 'Usuario',
        sortKey: 'username',
        render: (u) => (
          <div className="sm:min-w-36">
            <p className="font-medium text-slate-900">
              {u.username}
              {u.id === currentUserId && <span className="ml-1.5 text-xs font-normal text-slate-500">(usted)</span>}
            </p>
            {u.email && <p className="mt-0.5 text-xs text-slate-500">{u.email}</p>}
          </div>
        ),
      },
      { key: 'fullName', header: 'Nombre', sortKey: 'fullname' },
      { key: 'roleName', header: 'Perfil', sortKey: 'role' },
      { key: 'socialWorkerName', header: 'Trabajadora social', hideOnMobile: true, render: (u) => u.socialWorkerName ?? '—' },
      {
        key: 'lastLoginAt',
        header: 'Último acceso',
        sortKey: 'lastloginat',
        hideOnMobile: true,
        className: 'whitespace-nowrap',
        render: (u) => (u.lastLoginAt ? formatDateTime(u.lastLoginAt) : 'Nunca'),
      },
      {
        key: 'status',
        header: 'Estatus',
        sortKey: 'status',
        render: (u) => (
          <div className="flex flex-wrap gap-1">
            <StatusBadge statusId={u.statusId} statusName={u.statusName} />
            {u.isLockedOut && <Badge tone="danger">Bloqueado</Badge>}
            {u.mustChangePassword && <Badge tone="warning">Clave temporal</Badge>}
          </div>
        ),
      },
    ],
    fields: [
      {
        name: 'username',
        label: 'Usuario',
        type: 'text',
        required: true,
        maxLength: 50,
        readOnlyOnEdit: true,
        autoComplete: 'off',
        hint: 'Con el que inicia sesión. No se puede cambiar después.',
      },
      { name: 'fullName', label: 'Nombre completo', type: 'text', required: true, maxLength: 150 },
      { name: 'email', label: 'Correo electrónico', type: 'text', maxLength: 150, autoComplete: 'off' },
      { name: 'roleId', label: 'Perfil', type: 'lookup', required: true, lookupPath: '/users/roles' },
      { name: 'statusId', label: 'Estatus', type: 'status', required: true },
      {
        name: 'socialWorkerId',
        label: 'Trabajadora social vinculada',
        type: 'lookup',
        lookupPath: '/social-workers/lookup?withoutUser=true',
        placeholder: 'Sin vincular',
        hint: 'Opcional. Solo aparecen las que aún no tienen usuario.',
        currentOption: (u) => (u.socialWorkerId ? { value: u.socialWorkerId, label: u.socialWorkerName ?? '' } : null),
      },
      {
        name: 'password',
        label: 'Contraseña',
        type: 'password',
        required: true,
        only: 'create',
        hint: `${PASSWORD_HINT} Es temporal: el usuario deberá cambiarla en su primer ingreso.`,
      },
      { name: 'confirmPassword', label: 'Confirmar contraseña', type: 'password', required: true, only: 'create' },
    ],
    schema: editSchema,
    createSchema,
    emptyForm: {
      username: '',
      fullName: '',
      email: '',
      roleId: '',
      statusId: ACTIVE_STATUS_ID,
      socialWorkerId: '',
      password: '',
      confirmPassword: '',
    },
    toForm: (u) => ({
      username: u.username,
      fullName: u.fullName,
      email: u.email ?? '',
      roleId: u.roleId,
      statusId: u.statusId,
      socialWorkerId: u.socialWorkerId ?? '',
      password: '',
      confirmPassword: '',
    }),
    toPayload: (form, mode) => {
      const common = {
        fullName: form.fullName,
        email: form.email || null,
        roleId: form.roleId,
        statusId: form.statusId,
        socialWorkerId: form.socialWorkerId || null,
      }
      return mode === 'create' ? { ...common, username: form.username, password: form.password } : common
    },
    getLabel: (u) => u.username,
    filters: { status: true, parent: { label: 'Perfil', lookupPath: '/users/roles', param: 'roleId' } },
    canDeleteItem: (u) => u.id !== currentUserId,
    rowActions: (u) => {
      if (!canUpdate || u.id === currentUserId) return []
      const actions: RowAction[] = [
        { key: 'reset', label: 'Restablecer contraseña', icon: <KeyRound className="size-4" />, onClick: () => onResetPassword(u) },
      ]
      if (u.isLockedOut) {
        actions.push({ key: 'unlock', label: 'Desbloquear', icon: <LockOpen className="size-4" />, onClick: () => onUnlock(u) })
      }
      return actions
    },
  }
}
