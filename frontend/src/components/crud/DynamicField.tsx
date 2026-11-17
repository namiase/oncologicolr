import { Controller, type Control, type FieldValues, type UseFormRegister, type FieldError } from 'react-hook-form'
import { Field } from '@/components/ui/Field'
import { describedBy } from '@/components/ui/fieldStyles'
import { Input, Select, Textarea, type SelectOption } from '@/components/ui/Input'
import type { FieldConfig, FormMode } from './types'
import { useLookup, useStatuses } from './useCrudResource'

interface DynamicFieldProps<TForm extends FieldValues, TItem> {
  field: FieldConfig<TForm, TItem>
  item: TItem | null
  mode: FormMode
  idPrefix: string
  register: UseFormRegister<TForm>
  control: Control<TForm>
  error?: FieldError
  autoFocus?: boolean
}

/** Renderiza un control del formulario según su configuración (texto, número, select, lookup, estatus). */
export function DynamicField<TForm extends FieldValues, TItem>({
  field,
  item,
  mode,
  idPrefix,
  register,
  control,
  error,
  autoFocus,
}: DynamicFieldProps<TForm, TItem>) {
  const id = `${idPrefix}-${field.name}`
  const message = error?.message
  const common = {
    id,
    invalid: Boolean(message),
    autoFocus,
    'aria-describedby': describedBy(id, message, field.hint),
    'aria-required': field.required || undefined,
  }

  let control_: React.ReactNode
  switch (field.type) {
    case 'textarea':
      control_ = <Textarea {...common} maxLength={field.maxLength} placeholder={field.placeholder} {...register(field.name)} />
      break
    case 'password':
      control_ = (
        <Input
          {...common}
          type="password"
          maxLength={field.maxLength}
          autoComplete={field.autoComplete ?? 'new-password'}
          {...register(field.name)}
        />
      )
      break
    case 'number':
      control_ = <Input {...common} type="number" inputMode="numeric" placeholder={field.placeholder} {...register(field.name, { valueAsNumber: true })} />
      break
    case 'select':
    case 'lookup':
    case 'status':
      control_ = (
        <Controller
          control={control}
          name={field.name}
          render={({ field: f }) => (
            <OptionsSelect
              {...common}
              field={field}
              item={item}
              name={f.name}
              value={(f.value as string | undefined) ?? ''}
              onChange={f.onChange}
              onBlur={f.onBlur}
              ref={f.ref}
            />
          )}
        />
      )
      break
    default: {
      const readOnly = mode === 'edit' && field.readOnlyOnEdit
      control_ = (
        <Input
          {...common}
          type="text"
          maxLength={field.maxLength}
          placeholder={field.placeholder}
          autoComplete={field.autoComplete}
          readOnly={readOnly}
          className={readOnly ? 'bg-slate-50 text-slate-500' : undefined}
          {...register(field.name)}
        />
      )
    }
  }

  return (
    <Field id={id} label={field.label} required={field.required} error={message} hint={field.hint} className={field.fullWidth ? 'sm:col-span-2' : undefined}>
      {control_}
    </Field>
  )
}

interface OptionsSelectProps<TForm extends FieldValues, TItem> {
  id: string
  invalid: boolean
  autoFocus?: boolean
  'aria-describedby'?: string
  'aria-required'?: boolean
  field: FieldConfig<TForm, TItem>
  item: TItem | null
  name: string
  value: string
  onChange: (value: string) => void
  onBlur: () => void
  ref: React.Ref<HTMLSelectElement>
}

function OptionsSelect<TForm extends FieldValues, TItem>({ field, item, onChange, ...props }: OptionsSelectProps<TForm, TItem>) {
  const statuses = useStatuses()
  const lookup = useLookup(field.type === 'lookup' ? field.lookupPath : undefined)

  let options: SelectOption[] = field.options ?? []
  let loading = false
  if (field.type === 'status') {
    options = (statuses.data ?? []).map((s) => ({ value: s.id, label: s.name }))
    loading = statuses.isPending
  } else if (field.type === 'lookup') {
    options = (lookup.data ?? []).map((o) => ({ value: o.id, label: o.name, group: o.parentName ?? undefined }))
    loading = lookup.isPending
    // Si el valor actual ya no está activo, se mantiene visible para no perderlo al editar.
    const current = item && field.currentOption?.(item)
    if (current && !options.some((o) => o.value === current.value)) options = [current, ...options]
  }

  return (
    <Select
      {...props}
      options={options}
      disabled={loading}
      placeholder={loading ? 'Cargando…' : field.placeholder ?? 'Seleccione…'}
      onChange={(e) => onChange(e.target.value)}
    />
  )
}
