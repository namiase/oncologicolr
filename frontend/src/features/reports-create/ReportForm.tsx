import { useCallback, useEffect, useRef, useState, type ReactNode, type RefObject } from 'react'
import { Controller, get, useForm, useWatch, type Control, type Path, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { FileDown, History, TriangleAlert } from 'lucide-react'
import { toast } from 'sonner'
import { useLookup } from '@/components/crud/useCrudResource'
import { Button } from '@/components/ui/Button'
import { Field } from '@/components/ui/Field'
import { Input } from '@/components/ui/Input'
import type { SearchableOption } from '@/components/ui/SearchableSelect'
import { describedBy } from '@/components/ui/fieldStyles'
import { getErrorMessage, getFieldErrors } from '@/lib/errors'
import { useAuthStore } from '@/stores/authStore'
import type { LookupItem } from '@/types/api'
import { ChoiceField, PhoneListField, SelectField } from './ReportFields'
import { CountedTextarea, IdentityCardInput, Section } from './ReportInputs'
import {
  ageInYears,
  describeAge,
  emptyReportForm,
  formFieldOf,
  JOB_OTHER_OPTIONS,
  LIMITS,
  OTHER_OPTION_F,
  reportSchema,
  todayISO,
  toPayload,
  toReportForm,
  type ReportForm as ReportFormValues,
} from './reportSchema'
import { reportsApi, type SocialReport, type SocialReportContext } from './reportsApi'

type FieldName = Path<ReportFormValues>

interface ReportFormProps {
  mode: 'create' | 'edit'
  context: SocialReportContext
  /** Informe a editar (modo edición). */
  initial?: SocialReport
  onSaved: (report: SocialReport) => void | Promise<void>
}

const toOptions = (items: LookupItem[] | undefined): SearchableOption[] =>
  (items ?? []).map((item) => ({ value: item.id, label: item.name, keywords: item.code ?? undefined }))

/** Estados con el mismo nombre en varios países: se distinguen por el padre si hace falta. */
const YES_NO: SearchableOption[] = [
  { value: 'true', label: 'Sí' },
  { value: 'false', label: 'No' },
]

// ---- Borrador en la pestaña (sessionStorage) ----
// Protege la entrevista si la sesión se cierra por inactividad. Se guarda por usuario y
// se borra al guardar el informe o al descartarlo.

const draftKey = (userId: string | undefined) => (userId ? `ts:report-draft:v2:${userId}` : null)

/** Campos que se llenan solos: si solo cambian estos, no hay nada que recuperar. */
const AUTO_FIELDS = new Set<string>(['admissionDate', 'birthCountryId', 'isPediatric'])

const filled = (value: unknown): boolean =>
  typeof value === 'string'
    ? value.trim() !== ''
    : Array.isArray(value)
      ? value.some((row) => Object.values(row as object).some(filled))
      : false

const hasContent = (values: ReportFormValues) => Object.entries(values).some(([key, value]) => !AUTO_FIELDS.has(key) && filled(value))

function readDraft(key: string | null): ReportFormValues | null {
  if (!key) return null
  try {
    const raw = sessionStorage.getItem(key)
    if (!raw) return null
    const draft = { ...emptyReportForm(), ...(JSON.parse(raw) as Partial<ReportFormValues>) }
    return hasContent(draft) ? draft : null
  } catch {
    return null
  }
}

function writeDraft(key: string | null, values: ReportFormValues) {
  if (!key) return
  try {
    if (hasContent(values)) sessionStorage.setItem(key, JSON.stringify(values))
    else sessionStorage.removeItem(key)
  } catch {
    /* sin almacenamiento disponible: el formulario funciona igual, sin borrador */
  }
}

function clearDraft(key: string | null) {
  if (!key) return
  try {
    sessionStorage.removeItem(key)
  } catch {
    /* nada que limpiar */
  }
}

// ---------------------------------------------------------------------------------------

export function ReportForm({ mode, context, initial, onSaved }: ReportFormProps) {
  const userId = useAuthStore((s) => s.user?.id)
  const storageKey = mode === 'create' ? draftKey(userId) : null
  const [restoredDraft, setRestoredDraft] = useState(() => readDraft(storageKey))
  const isAdministrator = context.canChooseSocialWorker
  const cannotSign = mode === 'create' && !isAdministrator && !context.socialWorker

  const countries = useLookup('/catalogs/countries/lookup')
  const venezuelaId = countries.data?.find((c) => c.code === 'VE')?.id

  const defaults = (): ReportFormValues => {
    if (initial) return toReportForm(initial)
    const base = emptyReportForm()
    if (venezuelaId) base.birthCountryId = venezuelaId // la mayoría de los pacientes nació en Venezuela
    return base
  }

  const {
    register,
    control,
    handleSubmit,
    setError,
    setValue,
    getValues,
    reset,
    subscribe,
    formState: { errors, isSubmitting },
  } = useForm<ReportFormValues>({
    resolver: zodResolver(reportSchema) as unknown as Resolver<ReportFormValues>,
    defaultValues: restoredDraft ?? defaults(),
  })

  // Guarda el borrador poco después de cada cambio, sin volver a dibujar el formulario.
  useEffect(() => {
    if (!storageKey) return
    let timer: ReturnType<typeof setTimeout> | undefined
    const unsubscribe = subscribe({
      formState: { values: true },
      callback: ({ values }) => {
        clearTimeout(timer)
        timer = setTimeout(() => writeDraft(storageKey, values), 600)
      },
    })
    return () => {
      clearTimeout(timer)
      unsubscribe()
    }
  }, [storageKey, subscribe])

  const discardDraft = () => {
    clearDraft(storageKey)
    setRestoredDraft(null)
    reset(defaults())
  }

  // ---- Catálogos ----
  const [countryId, stateId, isPediatric] = useWatch({ control, name: ['birthCountryId', 'birthStateId', 'isPediatric'] })
  const religions = useLookup('/catalogs/religions/lookup')
  const genders = useLookup('/catalogs/genders/lookup')
  const races = useLookup('/catalogs/races/lookup')
  const maritalStatuses = useLookup('/catalogs/marital-statuses/lookup')
  const educationLevels = useLookup('/catalogs/education-levels/lookup')
  const professions = useLookup('/catalogs/professions/lookup')
  const occupations = useLookup('/catalogs/occupations/lookup')
  const kinships = useLookup('/catalogs/kinships/lookup')
  const areaCodes = useLookup('/catalogs/area-codes/lookup')
  const states = useLookup(countryId ? '/catalogs/states/lookup' : undefined, countryId || undefined)
  const municipalities = useLookup(stateId ? '/catalogs/municipalities/lookup' : undefined, stateId || undefined)

  // Venezuela preseleccionada en un informe nuevo (los países pueden llegar después de montar).
  useEffect(() => {
    if (mode === 'create' && venezuelaId && !getValues('birthCountryId')) setValue('birthCountryId', venezuelaId)
  }, [mode, venezuelaId, getValues, setValue])

  // "Paciente de pediatría" se marca sola según la edad hasta que la usuaria la cambie a mano.
  const pediatricManual = useRef(mode === 'edit')
  const setPediatric = useCallback((value: boolean) => setValue('isPediatric', value), [setValue])
  const markPediatricManual = useCallback(() => {
    pediatricManual.current = true
  }, [])

  const professionOptions: SearchableOption[] = [
    ...(professions.data ?? []).map((p) => ({ value: `p:${p.id}`, label: p.name, group: 'Profesiones' })),
    ...(occupations.data ?? []).map((o) => ({ value: `o:${o.id}`, label: o.name, group: 'Oficios' })),
  ]
  // El código de área se ve solo ("0414"); en la lista, con su operadora o ciudad.
  const areaCodeOptions: SearchableOption[] = (areaCodes.data ?? []).map((a) => ({
    value: a.id,
    label: a.name,
    short: a.code ?? a.name,
    keywords: a.code ?? undefined,
  }))
  const kinshipOptions = toOptions(kinships.data)

  // ---- Envío ----
  const submit = handleSubmit(
    async (values) => {
      try {
        const payload = toPayload(values)
        const saved = initial ? await reportsApi.update(initial.id, payload) : await reportsApi.create(payload)
        clearDraft(storageKey)
        setRestoredDraft(null)
        await onSaved(saved)
      } catch (error) {
        const known = new Set(Object.keys(emptyReportForm()))
        const fieldErrors = Object.entries(getFieldErrors(error))
          .map(([name, message]) => [formFieldOf(name), message] as const)
          .filter(([name]) => known.has(name.split('.')[0]!))
        fieldErrors.forEach(([name, message], index) =>
          setError(name as FieldName, { type: 'server', message }, { shouldFocus: index === 0 }),
        )
        if (fieldErrors.length === 0) setError('root.server', { type: 'server', message: getErrorMessage(error) })
        toast.error(fieldErrors.length > 0 ? 'Revise los campos marcados.' : getErrorMessage(error))
      }
    },
    () => toast.error('Revise los campos marcados.'),
  )

  // ---- Ayudantes de campos ----
  const errorOf = (name: FieldName) => (get(errors, name) as { message?: string } | undefined)?.message
  const id = (name: FieldName) => `rf-${name}`

  const text = (
    name: FieldName,
    label: string,
    maxLength: number,
    props: { className?: string; required?: boolean; placeholder?: string; type?: string; max?: string } = {},
  ) => {
    const error = errorOf(name)
    return (
      <Field id={id(name)} label={label} required={props.required} error={error} className={props.className}>
        <Input
          id={id(name)}
          type={props.type ?? 'text'}
          maxLength={maxLength}
          placeholder={props.placeholder}
          max={props.max}
          invalid={Boolean(error)}
          aria-describedby={describedBy(id(name), error)}
          {...register(name)}
        />
      </Field>
    )
  }

  const select = (
    name: FieldName,
    label: string,
    options: SearchableOption[],
    props: { className?: string; loading?: boolean; placeholder?: string; disabled?: boolean; onValueChange?: () => void } = {},
  ) => (
    <SelectField
      control={control}
      name={name}
      label={label}
      options={options}
      error={errorOf(name)}
      className={props.className}
      disabled={props.disabled}
      loading={props.loading}
      placeholder={props.placeholder}
      onValueChange={props.onValueChange}
    />
  )

  /** Lista de tabla maestra con opción "Otro" (el valor nuevo se agrega a la tabla). */
  const choice = (
    name: FieldName,
    otherName: FieldName,
    label: string,
    options: SearchableOption[],
    props: { className?: string; loading?: boolean; otherMax?: number; others?: typeof JOB_OTHER_OPTIONS } = {},
  ) => (
    <ChoiceField
      control={control}
      name={name}
      otherName={otherName}
      label={label}
      options={options}
      others={props.others}
      otherMax={props.otherMax ?? LIMITS.otherValue}
      error={errorOf(name)}
      otherError={errorOf(otherName)}
      className={props.className}
      loading={props.loading}
    />
  )

  const composite = (name: FieldName, label: string, Component: typeof IdentityCardInput, className?: string, hint?: string) => {
    const error = errorOf(name)
    return (
      <Field id={id(name)} label={label} error={error} hint={hint} className={className}>
        <Controller
          control={control}
          name={name}
          render={({ field }) => (
            <Component
              id={id(name)}
              value={field.value as string}
              onChange={field.onChange}
              onBlur={field.onBlur}
              invalid={Boolean(error)}
              describedBy={describedBy(id(name), error, hint)}
            />
          )}
        />
      </Field>
    )
  }

  const longText = (name: FieldName, label: string, maxLength: number, rows: number, hint?: string) => {
    const error = errorOf(name)
    const counterId = `${id(name)}-counter`
    return (
      <Field id={id(name)} label={label} error={error} hint={hint} className="sm:col-span-2 lg:col-span-6">
        <Controller
          control={control}
          name={name}
          render={({ field }) => (
            <CountedTextarea
              id={id(name)}
              rows={rows}
              maxLength={maxLength}
              counterId={counterId}
              value={field.value as string}
              onChange={field.onChange}
              onBlur={field.onBlur}
              ref={field.ref}
              invalid={Boolean(error)}
              aria-describedby={[describedBy(id(name), error, hint), counterId].filter(Boolean).join(' ')}
            />
          )}
        />
      </Field>
    )
  }

  const rootError = errors.root?.server?.message

  return (
    <form onSubmit={submit} noValidate className="space-y-5">
      {restoredDraft && (
        <Notice icon={<History className="size-4" />} tone="info">
          Se recuperó un borrador sin guardar de esta sesión.{' '}
          <button type="button" className="font-semibold underline underline-offset-2" onClick={discardDraft}>
            Descartar borrador
          </button>
        </Notice>
      )}
      {cannotSign && (
        <Notice icon={<TriangleAlert className="size-4" />} tone="warning">
          Su usuario no está vinculado a una trabajadora social, así que no puede firmar informes. Pida al administrador que lo
          vincule en Configuración → Usuarios.
        </Notice>
      )}
      {rootError && (
        <Notice icon={<TriangleAlert className="size-4" />} tone="danger">
          {rootError}
        </Notice>
      )}

      <Section number={1} title="Identificación del paciente" description="Hoja 1 del formulario.">
        {text('medicalRecordNumber', 'Historia clínica Nº', LIMITS.medicalRecordNumber, { required: true, className: 'lg:col-span-2' })}
        {text('admissionDate', 'Fecha de ingreso', 10, { required: true, type: 'date', max: todayISO(), className: 'lg:col-span-2' })}
        {choice('religionId', 'religionOther', 'Religión', toOptions(religions.data), {
          loading: religions.isPending,
          others: [OTHER_OPTION_F],
          className: 'lg:col-span-2',
        })}

        {text('lastNames', 'Apellidos', LIMITS.names, { required: true, className: 'lg:col-span-3' })}
        {text('firstNames', 'Nombres', LIMITS.names, { required: true, className: 'lg:col-span-3' })}

        {composite('identityCard', 'Cédula de identidad', IdentityCardInput, 'lg:col-span-3')}
        {text('passportNumber', 'Nº de pasaporte', LIMITS.passportNumber, { className: 'lg:col-span-3' })}

        {text('birthPlace', 'Lugar de nacimiento', LIMITS.birthPlace, { className: 'lg:col-span-2', placeholder: 'Ciudad o localidad' })}
        {select('birthCountryId', 'País', toOptions(countries.data), {
          loading: countries.isPending,
          className: 'lg:col-span-2',
          onValueChange: () => {
            setValue('birthStateId', '')
            setValue('birthMunicipalityId', '')
          },
        })}
        {select('birthStateId', 'Estado', toOptions(states.data), {
          loading: Boolean(countryId) && states.isPending,
          disabled: !countryId,
          placeholder: countryId ? 'Seleccione…' : 'Elija primero el país',
          className: 'lg:col-span-2',
          onValueChange: () => setValue('birthMunicipalityId', ''),
        })}
        {select('birthMunicipalityId', 'Municipio', toOptions(municipalities.data), {
          loading: Boolean(stateId) && municipalities.isPending,
          disabled: !stateId,
          placeholder: stateId ? 'Seleccione…' : 'Elija primero el estado',
          className: 'lg:col-span-2',
        })}
        {text('birthDate', 'Fecha de nacimiento', 10, { type: 'date', max: todayISO(), className: 'lg:col-span-2' })}
        <AgeField control={control} manualRef={pediatricManual} onPediatric={setPediatric} />

        {select('genderId', 'Sexo', toOptions(genders.data), { loading: genders.isPending, className: 'lg:col-span-2' })}
        {choice('raceId', 'raceOther', 'Raza', toOptions(races.data), {
          loading: races.isPending,
          others: [OTHER_OPTION_F],
          className: 'lg:col-span-2',
        })}
        {choice('maritalStatusId', 'maritalStatusOther', 'Estado civil', toOptions(maritalStatuses.data), {
          loading: maritalStatuses.isPending,
          className: 'lg:col-span-2',
        })}

        {choice('educationLevelId', 'educationLevelOther', 'Grado de instrucción', toOptions(educationLevels.data), {
          loading: educationLevels.isPending,
          className: 'lg:col-span-3',
        })}
        {choice('professionOrOccupation', 'professionOrOccupationOther', 'Profesión u oficio', professionOptions, {
          loading: professions.isPending || occupations.isPending,
          others: JOB_OTHER_OPTIONS,
          className: 'lg:col-span-3',
        })}

        {text('homeAddress', 'Dirección de habitación', LIMITS.address, { className: 'sm:col-span-2 lg:col-span-6' })}
        <PhoneListField
          control={control}
          errors={errors}
          name="phones"
          label="Teléfonos"
          max={LIMITS.patientPhones}
          areaCodes={areaCodeOptions}
          loading={areaCodes.isPending}
        />
      </Section>

      <Section number={2} title="Seguridad social y referencia">
        {select('hasIvss', 'IVSS', YES_NO, { className: 'lg:col-span-1', placeholder: 'Sin indicar' })}
        {select('hasIpasme', 'IPASME', YES_NO, { className: 'lg:col-span-1', placeholder: 'Sin indicar' })}
        {text('insurancePolicy', 'Póliza', LIMITS.socialSecurity, { className: 'lg:col-span-2' })}
        {text('otherSocialSecurity', 'Otros', LIMITS.socialSecurity, { className: 'lg:col-span-2' })}
        {text('referredBy', 'Referido por', LIMITS.fullLine, { className: 'sm:col-span-2 lg:col-span-6' })}
      </Section>

      <Section number={3} title="Trabajo">
        {text('workplace', 'Lugar de trabajo', LIMITS.fullLine, { className: 'sm:col-span-2 lg:col-span-4' })}
        {text('workSchedule', 'Horario', LIMITS.workSchedule, { className: 'lg:col-span-2' })}
        {text('workAddress', 'Dirección del trabajo', LIMITS.address, { className: 'sm:col-span-2 lg:col-span-6' })}
        <PhoneListField
          control={control}
          errors={errors}
          name="workPhones"
          label="Teléfonos del trabajo"
          max={LIMITS.workPhones}
          areaCodes={areaCodeOptions}
          loading={areaCodes.isPending}
        />
      </Section>

      <Section number={4} title="Paciente de pediatría">
        <div className="sm:col-span-2 lg:col-span-6">
          <label className="inline-flex cursor-pointer items-center gap-2.5 text-sm font-semibold text-slate-800">
            <Controller
              control={control}
              name="isPediatric"
              render={({ field }) => (
                <input
                  type="checkbox"
                  className="size-4 rounded accent-brand-700"
                  ref={field.ref}
                  name={field.name}
                  checked={field.value as boolean}
                  onBlur={field.onBlur}
                  onChange={(e) => {
                    markPediatricManual()
                    field.onChange(e.target.checked)
                  }}
                  aria-describedby="rf-isPediatric-hint"
                />
              )}
            />
            Es paciente de pediatría
          </label>
          <p id="rf-isPediatric-hint" className="mt-1 text-xs text-slate-500">
            Se marca sola cuando el paciente es menor de 18 años. Puede cambiarla.
          </p>
        </div>
        {isPediatric && (
          <>
            {text('guardianFullName', 'Representante (apellidos y nombres)', LIMITS.guardianName, {
              required: true,
              className: 'sm:col-span-2 lg:col-span-2',
            })}
            {composite('guardianIdentityCard', 'C.I. del representante', IdentityCardInput, 'lg:col-span-2')}
            {choice('guardianKinshipId', 'guardianKinshipOther', 'Parentesco', kinshipOptions, {
              loading: kinships.isPending,
              otherMax: LIMITS.otherKinship,
              className: 'lg:col-span-2',
            })}
          </>
        )}
      </Section>

      <Section number={5} title="Familiares o conocidos" description="En el Distrito Capital o en su localidad.">
        {text('contactAddress', 'Dirección', LIMITS.address, { className: 'sm:col-span-2 lg:col-span-6' })}
        {text('contactFullName', 'Apellidos y nombres', LIMITS.fullLine, { className: 'sm:col-span-2 lg:col-span-6' })}
        <PhoneListField
          control={control}
          errors={errors}
          name="contactPhones"
          label="Teléfonos y parentesco"
          max={LIMITS.contactPhones}
          areaCodes={areaCodeOptions}
          kinships={kinshipOptions}
          otherKinshipMax={LIMITS.otherKinship}
          loading={areaCodes.isPending || kinships.isPending}
        />
      </Section>

      <Section
        number={6}
        title="Evaluación social"
        description="Hoja 2 del formulario. El contador indica el espacio disponible en el informe impreso."
      >
        {longText('medicalSocialArea', 'Área médico social', LIMITS.medicalSocialArea, 4)}
        {longText('psychosocialArea', 'Área psicosocial', LIMITS.psychosocialArea, 5)}
        {longText('socioeconomicArea', 'Área socioeconómica', LIMITS.socioeconomicArea, 7)}
        {longText('physicalEnvironmentArea', 'Área físico ambiental', LIMITS.physicalEnvironmentArea, 5)}
      </Section>

      <Section number={7} title="Diagnóstico y referencia">
        {longText('clinicalDiagnosis', 'Diagnóstico clínico', LIMITS.clinicalDiagnosis, 2)}
        {longText('referralServices', 'Servicios a que refieren', LIMITS.referralServices, 2)}
        {longText('observations', 'Observaciones', LIMITS.observations, 3)}
      </Section>

      <Section number={8} title="Trabajador(a) social tratante" description={signerDescription(context, initial)}>
        <SignerSummary context={context} initial={initial} />
      </Section>

      <div className="sticky bottom-0 z-10 -mx-4 flex flex-col-reverse gap-2 border-t border-slate-200 bg-surface/95 px-4 py-3 backdrop-blur sm:-mx-6 sm:flex-row sm:items-center sm:justify-end sm:px-6">
        <p className="text-xs text-slate-500 sm:mr-auto">Fecha de hoy: {formatISODate(todayISO())}</p>
        <Button type="submit" size="lg" loading={isSubmitting} disabled={cannotSign} icon={<FileDown className="size-4" />}>
          {mode === 'create' ? 'Guardar y descargar PDF' : 'Guardar cambios y descargar PDF'}
        </Button>
      </div>
    </form>
  )
}

const formatISODate = (iso: string) => {
  const [y, m, d] = iso.split('-')
  return `${d}/${m}/${y}`
}

/**
 * Edad calculada a la fecha de ingreso. Mientras la usuaria no haya cambiado a mano la
 * casilla de pediatría (manualRef), la marca si el paciente es menor de 18.
 */
function AgeField({
  control,
  manualRef,
  onPediatric,
}: {
  control: Control<ReportFormValues>
  manualRef: RefObject<boolean>
  onPediatric: (value: boolean) => void
}) {
  const [birthDate, admissionDate] = useWatch({ control, name: ['birthDate', 'admissionDate'] })
  const age = describeAge(birthDate, admissionDate)
  const years = ageInYears(birthDate, admissionDate)

  useEffect(() => {
    if (!manualRef.current && years !== null) onPediatric(years < 18)
  }, [years, manualRef, onPediatric])

  return (
    <Field id="rf-age" label="Edad" hint="Se calcula con la fecha de nacimiento." className="lg:col-span-2">
      <Input id="rf-age" value={age ?? ''} placeholder="—" readOnly tabIndex={-1} aria-describedby="rf-age-hint" />
    </Field>
  )
}

/**
 * Una sola indicación por caso: si no hay trabajadora que mostrar, el texto de SignerSummary ya
 * explica que se elige al descargar, y la descripción de la sección no lo repite.
 */
function signerDescription(context: SocialReportContext, initial?: SocialReport) {
  if (!context.canChooseSocialWorker) return 'Firma el informe impreso: es la vinculada a su usuario.'
  const keepsReportWorker = initial && context.socialWorker?.id !== initial.socialWorkerId
  const shown = keepsReportWorker ? initial.socialWorkerName : context.socialWorker?.fullName
  return shown ? 'Al descargar el PDF puede elegir otra trabajadora social para la firma.' : undefined
}

function SignerSummary({ context, initial }: { context: SocialReportContext; initial?: SocialReport }) {
  const worker = context.socialWorker
  const full = 'sm:col-span-2 lg:col-span-6'

  // Al editar se muestra la del informe (no cambia aunque la edite otra persona).
  if (initial && worker?.id !== initial.socialWorkerId) {
    return initial.socialWorkerName ? (
      <p className={`text-sm font-semibold text-slate-900 ${full}`}>{initial.socialWorkerName}</p>
    ) : (
      <p className={`text-sm text-slate-500 ${full}`}>Sin trabajadora asignada: se elige al descargar el PDF.</p>
    )
  }
  if (!worker) {
    return (
      <p className={`text-sm text-slate-500 ${full}`}>
        {context.canChooseSocialWorker
          ? 'Su usuario no tiene trabajadora social vinculada: al descargar el PDF elegirá quién firma.'
          : 'Sin trabajadora social vinculada.'}
      </p>
    )
  }
  return (
    <dl className={`grid gap-x-6 gap-y-3 text-sm sm:grid-cols-2 lg:grid-cols-4 ${full}`}>
      <Detail label="Nombre" value={worker.fullName} />
      <Detail label="C.I." value={worker.identityCard} />
      <Detail label="Nº de registro ANTS" value={worker.antsRegistrationNumber} />
      <Detail label="Colegio de TS" value={worker.socialWorkersAssociation} />
    </dl>
  )
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs font-semibold text-slate-500">{label}</dt>
      <dd className="mt-0.5 font-medium text-slate-900">{value}</dd>
    </div>
  )
}

const noticeTones = {
  info: 'bg-brand-50 text-brand-800 ring-brand-200 dark:bg-brand-500/10 dark:text-brand-200 dark:ring-brand-500/30',
  warning: 'bg-amber-50 text-amber-900 ring-amber-200 dark:bg-amber-500/10 dark:text-amber-200 dark:ring-amber-500/30',
  danger: 'bg-red-50 text-red-700 ring-red-200 dark:bg-red-500/10 dark:text-red-300 dark:ring-red-500/30',
}

function Notice({ icon, tone, children }: { icon: ReactNode; tone: keyof typeof noticeTones; children: ReactNode }) {
  return (
    <div role={tone === 'info' ? 'status' : 'alert'} className={`flex items-start gap-2.5 rounded-xl px-4 py-3 text-sm ring-1 ${noticeTones[tone]}`}>
      <span className="mt-0.5 shrink-0" aria-hidden>
        {icon}
      </span>
      <p>{children}</p>
    </div>
  )
}
