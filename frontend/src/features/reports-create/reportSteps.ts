import type { ReportForm } from './reportSchema'

/**
 * Pasos del asistente de Crear informe. Agrupan las 8 secciones del formulario en papel:
 * cada campo pertenece a un solo paso (así se sabe a qué paso llevar a la usuaria si hay un error).
 * El último paso (Evaluación social, con la firma) es el que guarda.
 */
export interface ReportStep {
  title: string
  fields: (keyof ReportForm)[]
}

export const REPORT_STEPS: ReportStep[] = [
  {
    title: 'Paciente',
    fields: [
      'medicalRecordNumber',
      'admissionDate',
      'religionId',
      'religionOther',
      'lastNames',
      'firstNames',
      'identityCard',
      'passportNumber',
      'birthPlace',
      'birthCountryId',
      'birthStateId',
      'birthMunicipalityId',
      'birthDate',
      'genderId',
      'raceId',
      'raceOther',
      'maritalStatusId',
      'maritalStatusOther',
      'educationLevelId',
      'educationLevelOther',
      'professionOrOccupation',
      'professionOrOccupationOther',
      'homeAddress',
      'phones',
    ],
  },
  {
    title: 'Contacto y trabajo',
    fields: ['hasIvss', 'hasIpasme', 'insurancePolicy', 'otherSocialSecurity', 'referredBy', 'workplace', 'workSchedule', 'workAddress', 'workPhones'],
  },
  {
    title: 'Familia',
    fields: [
      'isPediatric',
      'guardianFullName',
      'guardianIdentityCard',
      'guardianKinshipId',
      'guardianKinshipOther',
      'contactAddress',
      'contactFullName',
      'contactPhones',
    ],
  },
  {
    title: 'Evaluación social',
    fields: [
      'medicalSocialArea',
      'psychosocialArea',
      'socioeconomicArea',
      'physicalEnvironmentArea',
      'clinicalDiagnosis',
      'referralServices',
      'observations',
    ],
  },
]

/** Nombre de los campos que pueden faltar al guardar, para decirle a la usuaria cuál es. */
const FIELD_LABELS: Partial<Record<keyof ReportForm, string>> = {
  medicalRecordNumber: 'Historia clínica Nº',
  admissionDate: 'Fecha de ingreso',
  lastNames: 'Apellidos',
  firstNames: 'Nombres',
  identityCard: 'Cédula de identidad',
  birthDate: 'Fecha de nacimiento',
  religionOther: 'Religión',
  raceOther: 'Raza',
  maritalStatusOther: 'Estado civil',
  educationLevelOther: 'Grado de instrucción',
  professionOrOccupationOther: 'Profesión u oficio',
  phones: 'Teléfonos',
  workPhones: 'Teléfonos del trabajo',
  guardianFullName: 'Representante',
  guardianIdentityCard: 'C.I. del representante',
  guardianKinshipOther: 'Parentesco del representante',
  contactPhones: 'Teléfonos y parentesco',
}

/** Todas las rutas de campos con error, en el orden del formulario. */
export function errorPaths(errors: object, path: string[] = []): string[] {
  const found: string[] = []
  for (const [key, value] of Object.entries(errors)) {
    if (key === 'ref' || !value || typeof value !== 'object') continue
    if ('message' in value && typeof (value as { message?: unknown }).message === 'string') found.push([...path, key].join('.'))
    else found.push(...errorPaths(value as object, [...path, key]))
  }
  return found
}

/** Rutas con error ordenadas por paso (las de campos que no son de ningún paso se descartan). */
export const errorPathsByStep = (errors: object) =>
  errorPaths(errors)
    .map((path, index) => ({ path, index, step: stepOfField(path) }))
    .filter(({ step }) => step >= 0)
    .sort((a, b) => a.step - b.step || a.index - b.index)
    .map(({ path }) => path)

/** "Falta completar «Apellidos» en el paso Paciente (y 2 campos más)." Recibe errorPathsByStep. */
export function missingFieldMessage(paths: string[]) {
  const first = paths[0]
  if (!first) return 'Revise los campos marcados.'
  const label = FIELD_LABELS[first.split('.')[0] as keyof ReportForm]
  const step = REPORT_STEPS[stepOfField(first)]!.title
  const rest = paths.length - 1
  const more = rest > 0 ? ` (y ${rest} ${rest === 1 ? 'campo más' : 'campos más'})` : ''
  return label ? `Falta completar o corregir «${label}» en el paso ${step}${more}.` : `Revise el paso ${step}${more}.`
}

export const LAST_STEP = REPORT_STEPS.length - 1

export type StepState = 'current' | 'done' | 'error' | 'todo'

/** Estado de cada paso: actual, revisado sin errores, con errores o todavía no visitado. */
export const stepStates = (current: number, furthest: number, stepErrors: boolean[]): StepState[] =>
  REPORT_STEPS.map((_, index) =>
    index === current ? 'current' : stepErrors[index] ? 'error' : index <= furthest ? 'done' : 'todo',
  )

/** Paso al que pertenece un campo ("phones.0.number" → paso de "phones"); -1 si ninguno. */
export const stepOfField = (name: string) =>
  REPORT_STEPS.findIndex((step) => step.fields.includes(name.split('.')[0] as keyof ReportForm))

/** Primer paso con algún error, según los nombres de campo con error. */
export const firstStepWithError = (fieldNames: string[]) => {
  const steps = fieldNames.map(stepOfField).filter((index) => index >= 0)
  return steps.length > 0 ? Math.min(...steps) : -1
}
