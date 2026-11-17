import { z } from 'zod'
import type { CrudEntity, CrudResource } from '@/components/crud'
import { StatusBadge } from '@/components/ui/Badge'
import { ACTIVE_STATUS_ID } from '@/types/api'

export interface SocialWorker extends CrudEntity {
  firstNames: string
  lastNames: string
  identityCard: string
  antsRegistrationNumber: string
  socialWorkersAssociation: string
  graduationYear: number
  userId: string | null
  username: string | null
  statusId: string
  statusName: string
}

export interface SocialWorkerForm {
  firstNames: string
  lastNames: string
  identityCard: string
  antsRegistrationNumber: string
  socialWorkersAssociation: string
  graduationYear: number
  statusId: string
}

const currentYear = new Date().getFullYear()

const schema = z.object({
  firstNames: z.string().trim().min(1, 'Los nombres son obligatorios.').max(100, 'Máximo 100 caracteres.'),
  lastNames: z.string().trim().min(1, 'Los apellidos son obligatorios.').max(100, 'Máximo 100 caracteres.'),
  identityCard: z
    .string()
    .trim()
    .min(1, 'La cédula es obligatoria.')
    .refine((v) => /^[VE]\d{5,9}$/i.test(v.replace(/[.\s-]/g, '')), 'Formato inválido. Ejemplo: V-12345678.'),
  antsRegistrationNumber: z
    .string()
    .trim()
    .min(1, 'El N° de registro ANTS es obligatorio.')
    .max(30, 'Máximo 30 caracteres.'),
  socialWorkersAssociation: z.string().trim().min(1, 'Indique el colegio.').max(200, 'Máximo 200 caracteres.'),
  graduationYear: z
    .number({ error: 'Ingrese el año.' })
    .int('Año inválido.')
    .min(1950, 'El año debe ser 1950 o posterior.')
    .max(currentYear, `El año no puede ser posterior a ${currentYear}.`),
  statusId: z.string().min(1, 'Seleccione el estatus.'),
})

/**
 * Trabajadoras Sociales usando el MISMO componente CRUD genérico de las tablas
 * maestras: solo cambia esta configuración (columnas, campos y validaciones).
 */
export const socialWorkerResource: CrudResource<SocialWorker, SocialWorkerForm> = {
  title: 'Trabajadoras sociales',
  singular: 'Trabajadora social',
  feminine: true,
  description: 'Registro del personal de trabajo social.',
  basePath: '/social-workers',
  searchPlaceholder: 'Buscar por nombre, cédula, N° ANTS o colegio…',
  columns: [
    {
      key: 'name',
      header: 'Nombre',
      sortKey: 'lastnames',
      render: (w) => (
        <div className="sm:min-w-44">
          <p className="font-medium text-slate-900">
            {w.lastNames}, {w.firstNames}
          </p>
          <p className="mt-0.5 text-xs text-slate-500 md:hidden">{w.identityCard}</p>
        </div>
      ),
    },
    { key: 'identityCard', header: 'Cédula', sortKey: 'identitycard', hideOnMobile: true, className: 'whitespace-nowrap tabular-nums' },
    { key: 'antsRegistrationNumber', header: 'N° ANTS', sortKey: 'antsregistrationnumber', hideOnMobile: true },
    { key: 'socialWorkersAssociation', header: 'Colegio', hideOnMobile: true, className: 'max-w-64 truncate' },
    { key: 'graduationYear', header: 'Año de grado', sortKey: 'graduationyear', hideOnMobile: true, className: 'tabular-nums' },
    {
      key: 'status',
      header: 'Estatus',
      sortKey: 'status',
      render: (w) => <StatusBadge statusId={w.statusId} statusName={w.statusName} />,
    },
  ],
  fields: [
    { name: 'firstNames', label: 'Nombres', type: 'text', required: true, maxLength: 100 },
    { name: 'lastNames', label: 'Apellidos', type: 'text', required: true, maxLength: 100 },
    { name: 'identityCard', label: 'Cédula de identidad', type: 'text', required: true, maxLength: 15, placeholder: 'V-12345678' },
    { name: 'antsRegistrationNumber', label: 'N° de registro ANTS', type: 'text', required: true, maxLength: 30 },
    {
      name: 'socialWorkersAssociation',
      label: 'Colegio de Trabajadores Sociales',
      type: 'text',
      required: true,
      maxLength: 200,
      fullWidth: true,
      placeholder: 'Colegio de Trabajadores Sociales del Distrito Capital',
    },
    { name: 'graduationYear', label: 'Año de grado', type: 'number', required: true, placeholder: String(currentYear) },
    { name: 'statusId', label: 'Estatus', type: 'status', required: true },
  ],
  schema,
  emptyForm: {
    firstNames: '',
    lastNames: '',
    identityCard: '',
    antsRegistrationNumber: '',
    socialWorkersAssociation: '',
    graduationYear: Number.NaN,
    statusId: ACTIVE_STATUS_ID,
  },
  toForm: (w) => ({
    firstNames: w.firstNames,
    lastNames: w.lastNames,
    identityCard: w.identityCard,
    antsRegistrationNumber: w.antsRegistrationNumber,
    socialWorkersAssociation: w.socialWorkersAssociation,
    graduationYear: w.graduationYear,
    statusId: w.statusId,
  }),
  getLabel: (w) => `${w.firstNames} ${w.lastNames}`,
  filters: { status: true },
}
