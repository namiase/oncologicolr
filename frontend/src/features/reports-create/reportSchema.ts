import { z } from 'zod'
import type { OtherOption, SearchableOption } from '@/components/ui/SearchableSelect'
import type { SocialReport, SocialReportPayload, SocialReportPhone, SocialReportPhoneInput } from './reportsApi'

/**
 * Longitudes máximas: garantizan que el PDF quepa en 2 hojas Carta.
 * Copia de SocialReportLimits (backend, Domain/Entities/Reports): si cambia un valor, cambiarlo en ambos lados.
 */
export const LIMITS = {
  medicalRecordNumber: 20,
  names: 60,
  passportNumber: 30,
  birthPlace: 40,
  address: 180,
  socialSecurity: 30,
  fullLine: 100,
  workSchedule: 60,
  guardianName: 50,
  phoneNumber: 7,
  patientPhones: 4,
  workPhones: 2,
  contactPhones: 6,
  otherValue: 40,
  otherKinship: 25,
  medicalSocialArea: 700,
  psychosocialArea: 1000,
  socioeconomicArea: 1500,
  physicalEnvironmentArea: 1000,
  clinicalDiagnosis: 250,
  referralServices: 250,
  observations: 500,
} as const

/** Valor de una lista cuando se eligió "Otro": el texto va en el campo "…Other". */
export const OTHER = '__other__'
/** En "Profesión u oficio" hay un "Otro" por grupo. */
export const OTHER_PROFESSION = `p:${OTHER}`
export const OTHER_OCCUPATION = `o:${OTHER}`

/** "Otro" de una lista: se escribe el valor nuevo y se agrega a la tabla maestra al guardar. */
export const OTHER_OPTION: OtherOption = {
  value: OTHER,
  label: 'Otro (escribir)',
  createLabel: (query) => `Agregar «${query}»`,
}

/** El mismo "Otro" para listas de nombre femenino (religión, raza). */
export const OTHER_OPTION_F: OtherOption = { ...OTHER_OPTION, label: 'Otra (escribir)' }

/**
 * Filas de catálogo llamadas solo "Otro"/"Otra": junto a "Otro (escribir)" serían dos opciones
 * casi iguales, y la del catálogo imprime "Otra" en el PDF sin el valor real. Se ocultan de la
 * lista salvo que sea el valor ya guardado en el informe.
 */
const GENERIC_OTHER = /^otr[oa]s?$/

export const withoutGenericOther = (options: SearchableOption[], current: string | undefined) =>
  options.filter(
    (option) =>
      option.value === current ||
      !GENERIC_OTHER.test(
        option.label
          .normalize('NFD')
          .replace(/[̀-ͯ]/g, '')
          .trim()
          .toLowerCase(),
      ),
  )

/** "Profesión u oficio": un "Otro" por grupo, para saber en qué tabla maestra agregarlo. */
export const JOB_OTHER_OPTIONS: OtherOption[] = [
  { value: OTHER_PROFESSION, label: 'Otra profesión (escribir)', createLabel: (q) => `Agregar «${q}» como profesión` },
  { value: OTHER_OCCUPATION, label: 'Otro oficio (escribir)', createLabel: (q) => `Agregar «${q}» como oficio` },
]

/** Fila de teléfono del formulario ('' = vacío; areaCodeId/kinshipId = OTHER si se escribió uno nuevo). */
export interface PhoneRow {
  areaCodeId: string
  areaCodeOther: string
  number: string
  kinshipId: string
  kinshipOther: string
}

export type PhoneListName = 'phones' | 'workPhones' | 'contactPhones'

/** Valores del formulario: todo es texto ('' = vacío), salvo la casilla de pediatría y los teléfonos. */
export interface ReportForm {
  medicalRecordNumber: string
  admissionDate: string
  religionId: string
  religionOther: string
  lastNames: string
  firstNames: string
  identityCard: string
  passportNumber: string
  birthPlace: string
  birthCountryId: string
  birthStateId: string
  birthMunicipalityId: string
  birthDate: string
  genderId: string
  raceId: string
  raceOther: string
  maritalStatusId: string
  maritalStatusOther: string
  educationLevelId: string
  educationLevelOther: string
  /** "p:<id>" (profesión), "o:<id>" (oficio), o OTHER_PROFESSION / OTHER_OCCUPATION. */
  professionOrOccupation: string
  professionOrOccupationOther: string
  homeAddress: string
  phones: PhoneRow[]
  hasIvss: '' | 'true' | 'false'
  hasIpasme: '' | 'true' | 'false'
  insurancePolicy: string
  otherSocialSecurity: string
  referredBy: string
  workplace: string
  workAddress: string
  workPhones: PhoneRow[]
  workSchedule: string
  isPediatric: boolean
  guardianFullName: string
  guardianIdentityCard: string
  guardianKinshipId: string
  guardianKinshipOther: string
  contactAddress: string
  contactFullName: string
  contactPhones: PhoneRow[]
  medicalSocialArea: string
  psychosocialArea: string
  socioeconomicArea: string
  physicalEnvironmentArea: string
  clinicalDiagnosis: string
  referralServices: string
  observations: string
}

// ---- Fechas (yyyy-mm-dd, sin zona horaria) ----

const pad = (n: number) => String(n).padStart(2, '0')

/** Fecha de hoy en la zona horaria del equipo. */
export const todayISO = () => {
  const now = new Date()
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

const parts = (iso: string) => iso.split('-').map(Number) as [number, number, number]

const monthsBetween = (birth: string, at: string) => {
  const [by, bm, bd] = parts(birth)
  const [ay, am, ad] = parts(at)
  return (ay - by) * 12 + (am - bm) - (ad < bd ? 1 : 0)
}

const isDate = (iso: string) => /^\d{4}-\d{2}-\d{2}$/.test(iso)

const toUtcArgs = (iso: string): [number, number, number] => {
  const [y, m, d] = parts(iso)
  return [y, m - 1, d]
}

/** "45 años", "8 meses", "12 días" a la fecha de ingreso. Copia de PatientAge (backend). */
export function describeAge(birth: string, at: string): string | null {
  if (!isDate(birth) || !isDate(at) || birth > at) return null
  const months = monthsBetween(birth, at)
  const years = Math.floor(months / 12)
  if (years >= 1) return years === 1 ? '1 año' : `${years} años`
  if (months >= 1) return months === 1 ? '1 mes' : `${months} meses`
  const days = Math.round((Date.UTC(...toUtcArgs(at)) - Date.UTC(...toUtcArgs(birth))) / 86_400_000)
  return days === 1 ? '1 día' : `${days} días`
}

/** Años cumplidos (null si falta una fecha). */
export const ageInYears = (birth: string, at: string) =>
  isDate(birth) && isDate(at) && birth <= at ? Math.floor(monthsBetween(birth, at) / 12) : null

// ---- Validación ----

const text = (max: number) => z.string().trim().max(max, `Máximo ${max} caracteres.`)
const required = (max: number, message: string) => text(max).min(1, message)
const pattern = (max: number, regex: RegExp, message: string) => text(max).refine((v) => v === '' || regex.test(v), message)

const IDENTITY_CARD = /^[VE]-\d{5,9}$/
const AREA_CODE = /^0\d{3}$/
const identityCard = pattern(15, IDENTITY_CARD, 'Cédula inválida. Ejemplo: V-12345678.')

export const emptyPhone = (): PhoneRow => ({ areaCodeId: '', areaCodeOther: '', number: '', kinshipId: '', kinshipOther: '' })

const isBlankPhone = (row: PhoneRow) =>
  !row.areaCodeId && !row.areaCodeOther.trim() && !row.number && !row.kinshipId && !row.kinshipOther.trim()

const phoneRow = z
  .object({
    areaCodeId: z.string(),
    areaCodeOther: z.string().trim(),
    number: z.string(),
    kinshipId: z.string(),
    kinshipOther: z.string().trim(),
  })
  .superRefine((row, ctx) => {
    if (isBlankPhone(row)) return
    if (!row.areaCodeId) ctx.addIssue({ code: 'custom', path: ['areaCodeId'], message: 'Elija el código de área.' })
    if (row.areaCodeId === OTHER && !AREA_CODE.test(row.areaCodeOther)) {
      ctx.addIssue({ code: 'custom', path: ['areaCodeOther'], message: 'Código de 4 dígitos que empieza por 0 (p. ej. 0261).' })
    }
    if (!/^\d{7}$/.test(row.number)) ctx.addIssue({ code: 'custom', path: ['number'], message: 'El número tiene 7 dígitos.' })
    if (row.kinshipId === OTHER) {
      if (!row.kinshipOther) ctx.addIssue({ code: 'custom', path: ['kinshipOther'], message: 'Escriba el parentesco.' })
      else if (row.kinshipOther.length > LIMITS.otherKinship) {
        ctx.addIssue({ code: 'custom', path: ['kinshipOther'], message: `Máximo ${LIMITS.otherKinship} caracteres.` })
      }
    }
  })

const phoneList = (max: number) =>
  z.array(phoneRow).refine((rows) => rows.filter((r) => !isBlankPhone(r)).length <= max, `Máximo ${max} teléfonos.`)

/** Listas con "Otro": si se eligió, el texto es obligatorio. [campo de la lista, campo del texto, máximo]. */
const CHOICES: [keyof ReportForm, keyof ReportForm, number][] = [
  ['religionId', 'religionOther', LIMITS.otherValue],
  ['raceId', 'raceOther', LIMITS.otherValue],
  ['maritalStatusId', 'maritalStatusOther', LIMITS.otherValue],
  ['educationLevelId', 'educationLevelOther', LIMITS.otherValue],
  ['professionOrOccupation', 'professionOrOccupationOther', LIMITS.otherValue],
  ['guardianKinshipId', 'guardianKinshipOther', LIMITS.otherKinship],
]

export const isOther = (value: string) => value === OTHER || value === OTHER_PROFESSION || value === OTHER_OCCUPATION

export const reportSchema = z
  .object({
    medicalRecordNumber: required(LIMITS.medicalRecordNumber, 'El N° de historia clínica es obligatorio.'),
    admissionDate: z.string().min(1, 'La fecha de ingreso es obligatoria.'),
    religionId: z.string(),
    religionOther: z.string().trim(),
    lastNames: required(LIMITS.names, 'Los apellidos son obligatorios.'),
    firstNames: required(LIMITS.names, 'Los nombres son obligatorios.'),
    identityCard,
    passportNumber: text(LIMITS.passportNumber),
    birthPlace: text(LIMITS.birthPlace),
    birthCountryId: z.string(),
    birthStateId: z.string(),
    birthMunicipalityId: z.string(),
    birthDate: z.string(),
    genderId: z.string(),
    raceId: z.string(),
    raceOther: z.string().trim(),
    maritalStatusId: z.string(),
    maritalStatusOther: z.string().trim(),
    educationLevelId: z.string(),
    educationLevelOther: z.string().trim(),
    professionOrOccupation: z.string(),
    professionOrOccupationOther: z.string().trim(),
    homeAddress: text(LIMITS.address),
    phones: phoneList(LIMITS.patientPhones),
    hasIvss: z.enum(['', 'true', 'false']),
    hasIpasme: z.enum(['', 'true', 'false']),
    insurancePolicy: text(LIMITS.socialSecurity),
    otherSocialSecurity: text(LIMITS.socialSecurity),
    referredBy: text(LIMITS.fullLine),
    workplace: text(LIMITS.fullLine),
    workAddress: text(LIMITS.address),
    workPhones: phoneList(LIMITS.workPhones),
    workSchedule: text(LIMITS.workSchedule),
    isPediatric: z.boolean(),
    guardianFullName: text(LIMITS.guardianName),
    guardianIdentityCard: identityCard,
    guardianKinshipId: z.string(),
    guardianKinshipOther: z.string().trim(),
    contactAddress: text(LIMITS.address),
    contactFullName: text(LIMITS.fullLine),
    contactPhones: phoneList(LIMITS.contactPhones),
    medicalSocialArea: text(LIMITS.medicalSocialArea),
    psychosocialArea: text(LIMITS.psychosocialArea),
    socioeconomicArea: text(LIMITS.socioeconomicArea),
    physicalEnvironmentArea: text(LIMITS.physicalEnvironmentArea),
    clinicalDiagnosis: text(LIMITS.clinicalDiagnosis),
    referralServices: text(LIMITS.referralServices),
    observations: text(LIMITS.observations),
  })
  .superRefine((form, ctx) => {
    if (form.admissionDate && form.admissionDate > todayISO()) {
      ctx.addIssue({ code: 'custom', path: ['admissionDate'], message: 'La fecha de ingreso no puede ser futura.' })
    }
    if (form.birthDate && form.admissionDate && form.birthDate > form.admissionDate) {
      ctx.addIssue({
        code: 'custom',
        path: ['birthDate'],
        message: 'La fecha de nacimiento debe ser anterior a la fecha de ingreso.',
      })
    }
    if (form.isPediatric && !form.guardianFullName) {
      ctx.addIssue({ code: 'custom', path: ['guardianFullName'], message: 'Indique el representante del paciente.' })
    }
    for (const [choice, other, max] of CHOICES) {
      if (!isOther(form[choice] as string)) continue
      if (choice === 'guardianKinshipId' && !form.isPediatric) continue
      const value = form[other] as string
      if (!value) ctx.addIssue({ code: 'custom', path: [other], message: 'Escriba el valor nuevo.' })
      else if (value.length > max) ctx.addIssue({ code: 'custom', path: [other], message: `Máximo ${max} caracteres.` })
    }
  })

// ---- Conversión formulario ⇄ API ----

export const emptyReportForm = (): ReportForm => ({
  medicalRecordNumber: '',
  admissionDate: todayISO(),
  religionId: '',
  religionOther: '',
  lastNames: '',
  firstNames: '',
  identityCard: '',
  passportNumber: '',
  birthPlace: '',
  birthCountryId: '',
  birthStateId: '',
  birthMunicipalityId: '',
  birthDate: '',
  genderId: '',
  raceId: '',
  raceOther: '',
  maritalStatusId: '',
  maritalStatusOther: '',
  educationLevelId: '',
  educationLevelOther: '',
  professionOrOccupation: '',
  professionOrOccupationOther: '',
  homeAddress: '',
  phones: [emptyPhone()],
  hasIvss: '',
  hasIpasme: '',
  insurancePolicy: '',
  otherSocialSecurity: '',
  referredBy: '',
  workplace: '',
  workAddress: '',
  workPhones: [emptyPhone()],
  workSchedule: '',
  isPediatric: false,
  guardianFullName: '',
  guardianIdentityCard: '',
  guardianKinshipId: '',
  guardianKinshipOther: '',
  contactAddress: '',
  contactFullName: '',
  contactPhones: [emptyPhone()],
  medicalSocialArea: '',
  psychosocialArea: '',
  socioeconomicArea: '',
  physicalEnvironmentArea: '',
  clinicalDiagnosis: '',
  referralServices: '',
  observations: '',
})

const yesNo = (value: boolean | null): ReportForm['hasIvss'] => (value === null ? '' : value ? 'true' : 'false')
const fromYesNo = (value: ReportForm['hasIvss']) => (value === '' ? null : value === 'true')
const orNull = (value: string) => (value.trim() === '' ? null : value.trim())

const toPhoneRows = (phones: SocialReportPhone[]): PhoneRow[] =>
  phones.length === 0
    ? [emptyPhone()]
    : phones.map((p) => ({ areaCodeId: p.areaCodeId, areaCodeOther: '', number: p.number, kinshipId: p.kinshipId ?? '', kinshipOther: '' }))

const toPhoneInputs = (rows: PhoneRow[]): SocialReportPhoneInput[] =>
  rows
    .filter((row) => !isBlankPhone(row))
    .map((row) => ({
      areaCodeId: row.areaCodeId && row.areaCodeId !== OTHER ? row.areaCodeId : null,
      areaCodeOther: row.areaCodeId === OTHER ? orNull(row.areaCodeOther) : null,
      number: row.number,
      kinshipId: row.kinshipId && row.kinshipId !== OTHER ? row.kinshipId : null,
      kinshipOther: row.kinshipId === OTHER ? orNull(row.kinshipOther) : null,
    }))

/** Id de la lista (null si se eligió "Otro") y texto nuevo (solo si se eligió "Otro"). */
const choice = (value: string, other: string) =>
  isOther(value) ? { id: null, other: orNull(other) } : { id: orNull(value), other: null }

export function toReportForm(report: SocialReport): ReportForm {
  const form = emptyReportForm()
  for (const key of Object.keys(form) as (keyof ReportForm)[]) {
    const value = (report as unknown as Record<string, unknown>)[key]
    if (typeof value === 'string') (form as unknown as Record<string, unknown>)[key] = value
  }
  return {
    ...form,
    hasIvss: yesNo(report.hasIvss),
    hasIpasme: yesNo(report.hasIpasme),
    isPediatric: report.isPediatric,
    professionOrOccupation: report.professionId
      ? `p:${report.professionId}`
      : report.occupationId
        ? `o:${report.occupationId}`
        : '',
    phones: toPhoneRows(report.phones),
    workPhones: toPhoneRows(report.workPhones),
    contactPhones: toPhoneRows(report.contactPhones),
  }
}

export function toPayload(form: ReportForm): SocialReportPayload {
  const religion = choice(form.religionId, form.religionOther)
  const race = choice(form.raceId, form.raceOther)
  const maritalStatus = choice(form.maritalStatusId, form.maritalStatusOther)
  const educationLevel = choice(form.educationLevelId, form.educationLevelOther)
  const guardianKinship = choice(form.guardianKinshipId, form.guardianKinshipOther)
  const job = form.professionOrOccupation
  const jobOther = orNull(form.professionOrOccupationOther)

  return {
    medicalRecordNumber: form.medicalRecordNumber.trim(),
    admissionDate: form.admissionDate,
    religionId: religion.id,
    religionOther: religion.other,
    lastNames: form.lastNames.trim(),
    firstNames: form.firstNames.trim(),
    identityCard: orNull(form.identityCard),
    passportNumber: orNull(form.passportNumber),
    birthPlace: orNull(form.birthPlace),
    birthCountryId: orNull(form.birthCountryId),
    birthStateId: orNull(form.birthStateId),
    birthMunicipalityId: orNull(form.birthMunicipalityId),
    birthDate: orNull(form.birthDate),
    genderId: orNull(form.genderId),
    raceId: race.id,
    raceOther: race.other,
    maritalStatusId: maritalStatus.id,
    maritalStatusOther: maritalStatus.other,
    educationLevelId: educationLevel.id,
    educationLevelOther: educationLevel.other,
    professionId: job.startsWith('p:') && job !== OTHER_PROFESSION ? job.slice(2) : null,
    professionOther: job === OTHER_PROFESSION ? jobOther : null,
    occupationId: job.startsWith('o:') && job !== OTHER_OCCUPATION ? job.slice(2) : null,
    occupationOther: job === OTHER_OCCUPATION ? jobOther : null,
    homeAddress: orNull(form.homeAddress),
    phones: toPhoneInputs(form.phones),
    hasIvss: fromYesNo(form.hasIvss),
    hasIpasme: fromYesNo(form.hasIpasme),
    insurancePolicy: orNull(form.insurancePolicy),
    otherSocialSecurity: orNull(form.otherSocialSecurity),
    referredBy: orNull(form.referredBy),
    workplace: orNull(form.workplace),
    workAddress: orNull(form.workAddress),
    workPhones: toPhoneInputs(form.workPhones),
    workSchedule: orNull(form.workSchedule),
    isPediatric: form.isPediatric,
    guardianFullName: orNull(form.guardianFullName),
    guardianIdentityCard: orNull(form.guardianIdentityCard),
    guardianKinshipId: guardianKinship.id,
    guardianKinshipOther: guardianKinship.other,
    contactAddress: orNull(form.contactAddress),
    contactFullName: orNull(form.contactFullName),
    contactPhones: toPhoneInputs(form.contactPhones),
    medicalSocialArea: orNull(form.medicalSocialArea),
    psychosocialArea: orNull(form.psychosocialArea),
    socioeconomicArea: orNull(form.socioeconomicArea),
    physicalEnvironmentArea: orNull(form.physicalEnvironmentArea),
    clinicalDiagnosis: orNull(form.clinicalDiagnosis),
    referralServices: orNull(form.referralServices),
    observations: orNull(form.observations),
  }
}

/**
 * Campo del formulario al que corresponde un error de la API. Las claves de los teléfonos llegan
 * como "contactPhones[2].number" y en el formulario son "contactPhones.2.number".
 */
export function formFieldOf(apiField: string): string {
  const aliases: Record<string, string> = {
    professionId: 'professionOrOccupation',
    occupationId: 'professionOrOccupation',
    professionOther: 'professionOrOccupationOther',
    occupationOther: 'professionOrOccupationOther',
    phones: 'phones.root',
    workPhones: 'workPhones.root',
    contactPhones: 'contactPhones.root',
  }
  // La validación automática del API escribe cada tramo en PascalCase ("ContactPhones[0].KinshipOther").
  const path = apiField
    .replace(/\[(\d+)\]/g, '.$1')
    .split('.')
    .map((segment) => segment.charAt(0).toLowerCase() + segment.slice(1))
    .join('.')
  return aliases[path] ?? path
}
