import { useEffect, useRef, useState, type KeyboardEvent, type RefCallback } from 'react'
import { Controller, get, useFieldArray, type Control, type FieldErrors, type Path } from 'react-hook-form'
import { Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Field } from '@/components/ui/Field'
import { Input } from '@/components/ui/Input'
import { Modal } from '@/components/ui/Modal'
import { SearchableSelect, type OtherOption, type SearchableOption } from '@/components/ui/SearchableSelect'
import { describedBy } from '@/components/ui/fieldStyles'
import {
  emptyPhone,
  isOther,
  OTHER,
  OTHER_OCCUPATION,
  OTHER_OPTION,
  OTHER_PROFESSION,
  withoutGenericOther,
  type PhoneListName,
  type PhoneRow,
  type ReportForm,
} from './reportSchema'

type FieldName = Path<ReportForm>

const errorOf = (errors: FieldErrors<ReportForm>, name: string) =>
  (get(errors, name) as { message?: string } | undefined)?.message

/** Una lista y su campo "…Other" comparten el botón: el foco de un error en el valor nuevo va a la lista. */
const bothRefs =
  (...refs: RefCallback<HTMLButtonElement>[]): RefCallback<HTMLButtonElement> =>
  (element) =>
    refs.forEach((ref) => ref(element))

interface OtherValueDialogProps {
  /** "Agregar religión" */
  title: string
  /** "Religión" */
  label: string
  initial: string
  maxLength: number
  placeholder?: string
  numeric?: boolean
  /** Mensaje de error si el texto no sirve; null si sirve. */
  validate?: (text: string) => string | null
  onCancel: () => void
  onConfirm: (text: string) => void
}

/**
 * Ventana para escribir el valor de "Otros". Se monta al abrirse y se desmonta al cerrar, así
 * empieza siempre con el valor actual. Enter confirma sin enviar el formulario del informe.
 */
function OtherValueDialog({ title, label, initial, maxLength, placeholder, numeric, validate, onCancel, onConfirm }: OtherValueDialogProps) {
  const [text, setText] = useState(initial)
  const [error, setError] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const id = 'rf-other-value'
  const hint = 'Se agregará a la tabla maestra al guardar el informe.'

  // El <dialog> enfoca su primer botón al abrirse; después se pasa el foco al campo.
  useEffect(() => {
    const frame = requestAnimationFrame(() => inputRef.current?.focus())
    return () => cancelAnimationFrame(frame)
  }, [])

  const confirm = () => {
    const value = text.replace(/\s+/g, ' ').trim()
    const problem = !value ? `Escriba ${label.toLowerCase()}.` : (validate?.(value) ?? null)
    if (problem) {
      setError(problem)
      inputRef.current?.focus()
      return
    }
    onConfirm(value)
  }

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key !== 'Enter') return
    event.preventDefault() // no enviar el formulario del informe que contiene la ventana
    confirm()
  }

  return (
    <Modal
      open
      onClose={onCancel}
      title={title}
      size="sm"
      footer={
        <>
          <Button variant="secondary" onClick={onCancel}>
            Cancelar
          </Button>
          <Button onClick={confirm}>Agregar</Button>
        </>
      }
    >
      <Field id={id} label={label} error={error ?? undefined} hint={hint}>
        <Input
          id={id}
          ref={inputRef}
          value={text}
          maxLength={maxLength}
          inputMode={numeric ? 'numeric' : undefined}
          placeholder={placeholder}
          autoComplete="off"
          invalid={Boolean(error)}
          aria-describedby={describedBy(id, error ?? undefined, hint)}
          onKeyDown={onKeyDown}
          onChange={(e) => {
            setText(numeric ? e.target.value.replace(/\D/g, '') : e.target.value)
            setError(null)
          }}
        />
      </Field>
    </Modal>
  )
}

/** Texto del campo cerrado cuando se eligió "Otros" y ya se escribió el valor: "Wicca (nuevo)". */
const newValueLabel = (text: string | undefined) => (text?.trim() ? `${text.trim()} (nuevo)` : undefined)

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

/** Lista con búsqueda de una tabla maestra (sin "Otros"): sexo, país, estado, municipio… */
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
  /** Campo donde queda el valor nuevo cuando se elige "Otros". */
  otherName: FieldName
  otherMax: number
  otherError?: string
  others?: OtherOption[]
}

/** Título y etiqueta de la ventana según el "Otros" elegido (en Profesión u oficio hay uno por tabla). */
const otherDialogText = (value: string, label: string) =>
  value === OTHER_PROFESSION
    ? { title: 'Agregar profesión', label: 'Profesión' }
    : value === OTHER_OCCUPATION
      ? { title: 'Agregar oficio', label: 'Oficio' }
      : { title: `Agregar ${label.toLowerCase()}`, label }

/**
 * Lista con búsqueda y opción "Otros": al elegirla se abre una ventana para escribir el valor, que
 * al guardar el informe se agrega a la tabla maestra. "Agregar «texto»" desde la búsqueda lo toma
 * directo. Para cambiar el valor escrito se vuelve a elegir "Otros".
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
  const message = error ?? otherError
  // Qué "Otros" se eligió mientras la ventana está abierta (el valor no cambia hasta Agregar).
  const [pending, setPending] = useState<string | null>(null)

  return (
    <Field id={id} label={label} error={message} className={className}>
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <Controller
            control={control}
            name={otherName}
            render={({ field: otherField }) => (
              <>
                <SearchableSelect
                  id={id}
                  ref={bothRefs(field.ref, otherField.ref)}
                  value={field.value as string}
                  displayValue={isOther(field.value as string) ? newValueLabel(otherField.value as string) : undefined}
                  options={withoutGenericOther(options, field.value as string)}
                  others={others}
                  placeholder={placeholder}
                  emptyLabel={emptyLabel ?? undefined}
                  loading={loading}
                  invalid={Boolean(message)}
                  describedBy={describedBy(id, message)}
                  onBlur={field.onBlur}
                  onChange={(next, typed) => {
                    if (typed !== undefined) {
                      field.onChange(next)
                      otherField.onChange(typed.slice(0, otherMax))
                    } else if (isOther(next)) {
                      setPending(next)
                    } else {
                      field.onChange(next)
                    }
                  }}
                />
                {pending && (
                  <OtherValueDialog
                    {...otherDialogText(pending, label)}
                    initial={field.value === pending ? (otherField.value as string) : ''}
                    maxLength={otherMax}
                    onCancel={() => setPending(null)}
                    onConfirm={(text) => {
                      field.onChange(pending)
                      otherField.onChange(text)
                      setPending(null)
                    }}
                  />
                )}
              </>
            )}
          />
        )}
      />
    </Field>
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
 * Teléfonos con código de área (tabla maestra, con "Otro código" para agregar uno, p. ej. 0261) y
 * el número de 7 dígitos. "Agregar otro teléfono" suma filas hasta el máximo que cabe en el PDF.
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
  // Ventana abierta para escribir un código de área o un parentesco nuevo.
  const [dialog, setDialog] = useState<'areaCode' | 'kinship' | null>(null)

  const messages = (['areaCodeId', 'areaCodeOther', 'number', 'kinshipId', 'kinshipOther'] as const)
    .map((key) => errorOf(errors, `${base}.${key}`))
    .filter(Boolean)
  const errorId = `${id}-error`
  const invalid = (...keys: (keyof PhoneRow)[]) => keys.some((key) => Boolean(errorOf(errors, `${base}.${key}`)))
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
                  <>
                    <SearchableSelect
                      id={`${id}-area`}
                      ref={bothRefs(field.ref, other.ref)}
                      ariaLabel={`Código de área del teléfono ${n}`}
                      value={field.value}
                      displayValue={field.value === OTHER ? other.value || undefined : undefined}
                      options={areaCodes}
                      others={[AREA_CODE_OTHER]}
                      placeholder="Código"
                      loading={loading}
                      invalid={invalid('areaCodeId', 'areaCodeOther')}
                      describedBy={describe}
                      onBlur={field.onBlur}
                      onChange={(value, typed) => {
                        if (value !== OTHER) field.onChange(value)
                        else if (typed !== undefined && AREA_CODE.test(typed)) {
                          field.onChange(OTHER)
                          other.onChange(typed)
                        } else setDialog('areaCode')
                      }}
                    />
                    {dialog === 'areaCode' && (
                      <OtherValueDialog
                        title="Agregar código de área"
                        label="Código de área"
                        initial={field.value === OTHER ? other.value : ''}
                        maxLength={4}
                        numeric
                        placeholder="0261"
                        validate={(text) => (AREA_CODE.test(text) ? null : 'Código de 4 dígitos que empieza por 0 (p. ej. 0261).')}
                        onCancel={() => setDialog(null)}
                        onConfirm={(text) => {
                          field.onChange(OTHER)
                          other.onChange(text)
                          setDialog(null)
                        }}
                      />
                    )}
                  </>
                )}
              />
            )}
          />
        </div>
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
          <div className="min-w-40 flex-1 sm:w-52 sm:flex-none">
            <Controller
              control={control}
              name={`${base}.kinshipId`}
              render={({ field }) => (
                <Controller
                  control={control}
                  name={`${base}.kinshipOther`}
                  render={({ field: other }) => (
                    <>
                      <SearchableSelect
                        id={`${id}-kinship`}
                        ref={bothRefs(field.ref, other.ref)}
                        ariaLabel={`Parentesco del teléfono ${n}`}
                        value={field.value}
                        displayValue={field.value === OTHER ? newValueLabel(other.value) : undefined}
                        options={withoutGenericOther(kinships, field.value)}
                        others={[OTHER_OPTION]}
                        placeholder="Parentesco"
                        emptyLabel="Sin indicar"
                        loading={loading}
                        invalid={invalid('kinshipId', 'kinshipOther')}
                        describedBy={describe}
                        onBlur={field.onBlur}
                        onChange={(value, typed) => {
                          if (value !== OTHER) field.onChange(value)
                          else if (typed !== undefined) {
                            field.onChange(OTHER)
                            other.onChange(typed.slice(0, otherKinshipMax))
                          } else setDialog('kinship')
                        }}
                      />
                      {dialog === 'kinship' && (
                        <OtherValueDialog
                          title="Agregar parentesco"
                          label="Parentesco"
                          initial={field.value === OTHER ? other.value : ''}
                          maxLength={otherKinshipMax}
                          onCancel={() => setDialog(null)}
                          onConfirm={(text) => {
                            field.onChange(OTHER)
                            other.onChange(text)
                            setDialog(null)
                          }}
                        />
                      )}
                    </>
                  )}
                />
              )}
            />
          </div>
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
