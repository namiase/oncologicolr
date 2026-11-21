import { useRef, type RefCallback } from 'react'
import { Controller, get, useFieldArray, useWatch, type Control, type FieldErrors, type Path } from 'react-hook-form'
import { Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Field } from '@/components/ui/Field'
import { Input } from '@/components/ui/Input'
import { SearchableSelect, type OtherOption, type SearchableOption } from '@/components/ui/SearchableSelect'
import { describedBy } from '@/components/ui/fieldStyles'
import { cn } from '@/lib/cn'
import {
  emptyPhone,
  isOther,
  OTHER,
  OTHER_OPTION,
  withoutGenericOther,
  type PhoneListName,
  type PhoneRow,
  type ReportForm,
} from './reportSchema'

type FieldName = Path<ReportForm>

const errorOf = (errors: FieldErrors<ReportForm>, name: string) =>
  (get(errors, name) as { message?: string } | undefined)?.message

/**
 * Al elegir "Otro" de la lista, el foco pasa al campo para escribir el valor (que aparece en ese
 * momento): `request()` en el onChange de la lista y `ref(fieldRef)` en el campo.
 */
function useFocusOnRequest() {
  const pending = useRef(false)
  const mounted = useRef<HTMLInputElement | null>(null)
  return {
    request: () => {
      // Si el campo ya estaba visible, se enfoca después de que la lista devuelva el foco a su botón.
      const element = mounted.current
      if (element) requestAnimationFrame(() => element.focus())
      else pending.current = true
    },
    ref:
      (fieldRef: RefCallback<HTMLInputElement>): RefCallback<HTMLInputElement> =>
      (element) => {
        fieldRef(element)
        mounted.current = element
        if (element && pending.current) {
          pending.current = false
          element.focus()
        }
      },
  }
}

interface SelectFieldProps {
  control: Control<ReportForm>
  name: FieldName
  label: string
  options: SearchableOption[]
  error?: string
  className?: string
  loading?: boolean
  disabled?: boolean
  placeholder?: string
  /** Opción para dejarlo vacío; null si no se puede vaciar. */
  emptyLabel?: string | null
  onValueChange?: (value: string) => void
}

/** Lista con búsqueda de una tabla maestra (sin "Otro"): sexo, país, estado, municipio… */
export function SelectField({
  control,
  name,
  label,
  options,
  error,
  className,
  loading,
  disabled,
  placeholder,
  emptyLabel = 'Sin indicar',
  onValueChange,
}: SelectFieldProps) {
  const id = `rf-${name}`
  return (
    <Field id={id} label={label} error={error} className={className}>
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <SearchableSelect
            id={id}
            ref={field.ref}
            value={field.value as string}
            options={options}
            placeholder={placeholder}
            emptyLabel={emptyLabel ?? undefined}
            loading={loading}
            disabled={disabled}
            invalid={Boolean(error)}
            describedBy={describedBy(id, error)}
            onBlur={field.onBlur}
            onChange={(value) => {
              field.onChange(value)
              onValueChange?.(value)
            }}
          />
        )}
      />
    </Field>
  )
}

interface ChoiceFieldProps extends SelectFieldProps {
  /** Campo donde se escribe el valor nuevo cuando se elige "Otro". */
  otherName: FieldName
  otherMax: number
  otherError?: string
  others?: OtherOption[]
}

/**
 * Lista con búsqueda y opción "Otro": al elegirla (o "Agregar «texto»" desde la búsqueda) aparece
 * un campo para escribir el valor, que al guardar se agrega a la tabla maestra.
 */
export function ChoiceField({
  control,
  name,
  otherName,
  label,
  options,
  others = [OTHER_OPTION],
  otherMax,
  error,
  otherError,
  className,
  loading,
  placeholder,
  emptyLabel = 'Sin indicar',
}: ChoiceFieldProps) {
  const id = `rf-${name}`
  const otherId = `rf-${otherName}`
  const otherHint = 'Se agregará a la tabla maestra al guardar.'
  const value = useWatch({ control, name }) as string
  const otherFocus = useFocusOnRequest()

  return (
    <div className={cn('flex flex-col gap-2', className)}>
      <Field id={id} label={label} error={error}>
        <Controller
          control={control}
          name={name}
          render={({ field }) => (
            <Controller
              control={control}
              name={otherName}
              render={({ field: otherField }) => (
                <SearchableSelect
                  id={id}
                  ref={field.ref}
                  value={field.value as string}
                  options={withoutGenericOther(options, field.value as string)}
                  others={others}
                  placeholder={placeholder}
                  emptyLabel={emptyLabel ?? undefined}
                  loading={loading}
                  invalid={Boolean(error)}
                  describedBy={describedBy(id, error)}
                  onBlur={field.onBlur}
                  onChange={(next, typed) => {
                    field.onChange(next)
                    if (typed !== undefined) otherField.onChange(typed.slice(0, otherMax))
                    else if (isOther(next)) otherFocus.request()
                  }}
                />
              )}
            />
          )}
        />
      </Field>
      {isOther(value) && (
        <Field id={otherId} label={`Escriba ${label.toLowerCase()}`} error={otherError} hint={otherHint}>
          <Controller
            control={control}
            name={otherName}
            render={({ field }) => (
              <Input
                id={otherId}
                ref={otherFocus.ref(field.ref)}
                value={field.value as string}
                maxLength={otherMax}
                placeholder="Valor nuevo"
                invalid={Boolean(otherError)}
                aria-describedby={describedBy(otherId, otherError, otherHint)}
                onBlur={field.onBlur}
                onChange={(e) => field.onChange(e.target.value)}
              />
            )}
          />
        </Field>
      )}
    </div>
  )
}

const AREA_CODE = /^0\d{3}$/

const AREA_CODE_OTHER: OtherOption = {
  value: OTHER,
  label: 'Otro código',
  createLabel: (query) => (AREA_CODE.test(query) ? `Agregar el código ${query}` : 'Otro código'),
}

interface PhoneListFieldProps {
  control: Control<ReportForm>
  errors: FieldErrors<ReportForm>
  name: PhoneListName
  label: string
  max: number
  areaCodes: SearchableOption[]
  /** Solo en los teléfonos de familiares: parentesco por teléfono. */
  kinships?: SearchableOption[]
  loading?: boolean
  otherKinshipMax?: number
}

/**
 * Teléfonos con código de área (tabla maestra, con "Otro" para agregar uno, p. ej. 0261) y el
 * número de 7 dígitos. "Agregar otro teléfono" suma filas hasta el máximo que cabe en el PDF.
 */
export function PhoneListField({
  control,
  errors,
  name,
  label,
  max,
  areaCodes,
  kinships,
  loading,
  otherKinshipMax = 25,
}: PhoneListFieldProps) {
  const { fields, append, remove, update } = useFieldArray({ control, name })
  const listError = errorOf(errors, `${name}.root`) ?? errorOf(errors, name)

  return (
    <fieldset className="sm:col-span-2 lg:col-span-6">
      <legend className="text-sm font-semibold text-slate-800">{label}</legend>
      <div className="mt-1.5 space-y-3">
        {fields.map((item, index) => (
          <PhoneRowFields
            key={item.id}
            control={control}
            errors={errors}
            name={name}
            index={index}
            areaCodes={areaCodes}
            kinships={kinships}
            loading={loading}
            otherKinshipMax={otherKinshipMax}
            onRemove={() => (fields.length > 1 ? remove(index) : update(index, emptyPhone()))}
          />
        ))}
      </div>
      {listError && (
        <p className="mt-1.5 text-xs font-medium text-red-600 dark:text-red-400" role="alert">
          {listError}
        </p>
      )}
      <div className="mt-2 flex items-center gap-3">
        <Button variant="ghost" size="sm" icon={<Plus className="size-4" />} disabled={fields.length >= max} onClick={() => append(emptyPhone())}>
          Agregar otro teléfono
        </Button>
        <span className="text-xs text-slate-500">
          {fields.length} de {max}
        </span>
      </div>
    </fieldset>
  )
}

interface PhoneRowFieldsProps {
  control: Control<ReportForm>
  errors: FieldErrors<ReportForm>
  name: PhoneListName
  index: number
  areaCodes: SearchableOption[]
  kinships?: SearchableOption[]
  loading?: boolean
  otherKinshipMax: number
  onRemove: () => void
}

function PhoneRowFields({ control, errors, name, index, areaCodes, kinships, loading, otherKinshipMax, onRemove }: PhoneRowFieldsProps) {
  const base = `${name}.${index}` as const
  const n = index + 1
  const id = `rf-${name}-${index}`
  const row = useWatch({ control, name: base }) as PhoneRow | undefined
  const areaCodeFocus = useFocusOnRequest()
  const kinshipFocus = useFocusOnRequest()

  const messages = (['areaCodeId', 'areaCodeOther', 'number', 'kinshipId', 'kinshipOther'] as const)
    .map((key) => errorOf(errors, `${base}.${key}`))
    .filter(Boolean)
  const errorId = `${id}-error`
  const invalid = (key: keyof PhoneRow) => Boolean(errorOf(errors, `${base}.${key}`))
  const digitsOnly = (value: string, max: number) => value.replace(/\D/g, '').slice(0, max)

  const describe = messages.length > 0 ? errorId : undefined

  return (
    <div>
      {/* En celular el número y el parentesco se estiran para que el botón de quitar quede en la misma fila. */}
      <div className="flex flex-wrap items-start gap-2">
        <div className="w-28 shrink-0 sm:w-32">
          <Controller
            control={control}
            name={`${base}.areaCodeId`}
            render={({ field }) => (
              <Controller
                control={control}
                name={`${base}.areaCodeOther`}
                render={({ field: other }) => (
                  <SearchableSelect
                    id={`${id}-area`}
                    ref={field.ref}
                    ariaLabel={`Código de área del teléfono ${n}`}
                    value={field.value}
                    options={areaCodes}
                    others={[AREA_CODE_OTHER]}
                    placeholder="Código"
                    loading={loading}
                    invalid={invalid('areaCodeId')}
                    describedBy={describe}
                    onBlur={field.onBlur}
                    onChange={(value, typed) => {
                      field.onChange(value)
                      if (typed !== undefined && AREA_CODE.test(typed)) other.onChange(typed)
                      else if (value === OTHER) areaCodeFocus.request()
                    }}
                  />
                )}
              />
            )}
          />
        </div>
        {row?.areaCodeId === OTHER && (
          <Controller
            control={control}
            name={`${base}.areaCodeOther`}
            render={({ field }) => (
              <Input
                ref={areaCodeFocus.ref(field.ref)}
                aria-label={`Código de área nuevo del teléfono ${n}`}
                inputMode="numeric"
                placeholder="0261"
                className="w-24"
                value={field.value}
                invalid={invalid('areaCodeOther')}
                aria-describedby={describe}
                onBlur={field.onBlur}
                onChange={(e) => field.onChange(digitsOnly(e.target.value, 4))}
              />
            )}
          />
        )}
        <Controller
          control={control}
          name={`${base}.number`}
          render={({ field }) => (
            <Input
              ref={field.ref}
              aria-label={`Número del teléfono ${n}`}
              inputMode="numeric"
              autoComplete="off"
              placeholder="1234567"
              className="w-auto min-w-28 flex-1 sm:w-32 sm:flex-none"
              value={field.value}
              invalid={invalid('number')}
              aria-describedby={describe}
              onBlur={field.onBlur}
              onChange={(e) => field.onChange(digitsOnly(e.target.value, 7))}
            />
          )}
        />
        {kinships && (
          <>
            <div className="min-w-40 flex-1 sm:w-52 sm:flex-none">
              <Controller
                control={control}
                name={`${base}.kinshipId`}
                render={({ field }) => (
                  <Controller
                    control={control}
                    name={`${base}.kinshipOther`}
                    render={({ field: other }) => (
                      <SearchableSelect
                        id={`${id}-kinship`}
                        ref={field.ref}
                        ariaLabel={`Parentesco del teléfono ${n}`}
                        value={field.value}
                        options={withoutGenericOther(kinships, field.value)}
                        others={[OTHER_OPTION]}
                        placeholder="Parentesco"
                        emptyLabel="Sin indicar"
                        loading={loading}
                        invalid={invalid('kinshipId')}
                        describedBy={describe}
                        onBlur={field.onBlur}
                        onChange={(value, typed) => {
                          field.onChange(value)
                          if (typed !== undefined) other.onChange(typed.slice(0, otherKinshipMax))
                          else if (value === OTHER) kinshipFocus.request()
                        }}
                      />
                    )}
                  />
                )}
              />
            </div>
            {row?.kinshipId === OTHER && (
              <Controller
                control={control}
                name={`${base}.kinshipOther`}
                render={({ field }) => (
                  <Input
                    ref={kinshipFocus.ref(field.ref)}
                    aria-label={`Parentesco nuevo del teléfono ${n}`}
                    placeholder="Parentesco"
                    maxLength={otherKinshipMax}
                    className="w-auto min-w-40 flex-1 sm:w-44 sm:flex-none"
                    value={field.value}
                    invalid={invalid('kinshipOther')}
                    aria-describedby={describe}
                    onBlur={field.onBlur}
                    onChange={(e) => field.onChange(e.target.value)}
                  />
                )}
              />
            )}
          </>
        )}
        <Button variant="ghost" size="icon" title="Quitar teléfono" aria-label={`Quitar el teléfono ${n}`} onClick={onRemove}>
          <Trash2 className="size-4" />
        </Button>
      </div>
      {messages.length > 0 && (
        <p id={errorId} className="mt-1 text-xs font-medium text-red-600 dark:text-red-400" role="alert">
          {messages.join(' ')}
        </p>
      )}
    </div>
  )
}
