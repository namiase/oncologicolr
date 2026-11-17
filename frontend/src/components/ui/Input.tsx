import type { ComponentProps } from 'react'
import { cn } from '@/lib/cn'
import { controlClass } from './fieldStyles'

interface InputProps extends ComponentProps<'input'> {
  invalid?: boolean
}

export function Input({ invalid, className, ...props }: InputProps) {
  return <input aria-invalid={invalid || undefined} className={cn(controlClass(invalid), 'h-10', className)} {...props} />
}

export function Textarea({ invalid, className, ...props }: ComponentProps<'textarea'> & { invalid?: boolean }) {
  return (
    <textarea
      aria-invalid={invalid || undefined}
      className={cn(controlClass(invalid), 'min-h-20 py-2', className)}
      {...props}
    />
  )
}

export interface SelectOption {
  value: string
  label: string
  /** Si se indica, las opciones se agrupan (<optgroup>), p. ej. estados por país. */
  group?: string
}

interface SelectProps extends ComponentProps<'select'> {
  invalid?: boolean
  options: SelectOption[]
  placeholder?: string
}

export function Select({ invalid, options, placeholder, className, ...props }: SelectProps) {
  return (
    <select aria-invalid={invalid || undefined} className={cn(controlClass(invalid), 'h-10 pr-8', className)} {...props}>
      {placeholder !== undefined && <option value="">{placeholder}</option>}
      {groupOptions(options).map(({ group, items }, index) =>
        group ? (
          <optgroup key={group + index} label={group}>
            {items.map(renderOption)}
          </optgroup>
        ) : (
          items.map(renderOption)
        ),
      )}
    </select>
  )
}

const renderOption = (o: SelectOption) => (
  <option key={o.value} value={o.value}>
    {o.label}
  </option>
)

/** Agrupa opciones consecutivas con el mismo grupo, conservando el orden recibido. */
function groupOptions(options: SelectOption[]) {
  const groups: { group?: string; items: SelectOption[] }[] = []
  for (const option of options) {
    const last = groups.at(-1)
    if (last && last.group === option.group) last.items.push(option)
    else groups.push({ group: option.group, items: [option] })
  }
  return groups
}
