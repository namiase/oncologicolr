import { useState, type ComponentProps, type ReactNode } from 'react'
import { ChevronDown } from 'lucide-react'
import { Card } from '@/components/ui/Feedback'
import { Input, Textarea } from '@/components/ui/Input'
import { controlClass } from '@/components/ui/fieldStyles'
import { cn } from '@/lib/cn'

/** Sección numerada del formulario, en el mismo orden que el formulario en papel. */
export function Section({
  number,
  title,
  description,
  children,
}: {
  number: number
  title: string
  description?: string
  children: ReactNode
}) {
  const headingId = `seccion-${number}`
  return (
    <Card className="p-5 sm:p-6">
      <section aria-labelledby={headingId}>
        <div className="mb-5 flex items-start gap-3">
          <span
            className="flex size-7 shrink-0 items-center justify-center rounded-full bg-brand-50 text-xs font-bold text-brand-700 dark:bg-brand-500/15 dark:text-brand-300"
            aria-hidden
          >
            {number}
          </span>
          <div>
            <h2 id={headingId} className="text-base font-bold text-slate-900">
              {title}
            </h2>
            {description && <p className="mt-0.5 text-sm text-slate-500">{description}</p>}
          </div>
        </div>
        <div className="grid gap-x-4 gap-y-5 sm:grid-cols-2 lg:grid-cols-6">{children}</div>
      </section>
    </Card>
  )
}

type Prefix = 'V' | 'E'

interface CompositeInputProps {
  id: string
  value: string
  onChange: (value: string) => void
  onBlur?: () => void
  invalid?: boolean
  describedBy?: string
}

/** Cédula: nacionalidad (V/E) + número. El valor es "V-12345678" o '' si no hay número. */
export function IdentityCardInput({ id, value, onChange, onBlur, invalid, describedBy }: CompositeInputProps) {
  const parsed = /^([VE])-(\d*)$/.exec(value)
  const [localPrefix, setLocalPrefix] = useState<Prefix>('V')
  const prefix = (parsed?.[1] as Prefix | undefined) ?? localPrefix
  const number = parsed?.[2] ?? ''
  const emit = (p: Prefix, n: string) => onChange(n ? `${p}-${n}` : '')

  return (
    <div className="flex gap-2">
      {/* Flecha propia (appearance-none) para verse igual que las listas con búsqueda del formulario. */}
      <div className="relative w-18 shrink-0">
        <select
          aria-label="Nacionalidad"
          value={prefix}
          aria-invalid={invalid || undefined}
          aria-describedby={describedBy}
          onBlur={onBlur}
          onChange={(e) => {
            const next = e.target.value as Prefix
            setLocalPrefix(next)
            emit(next, number)
          }}
          className={cn(controlClass(invalid), 'h-10 appearance-none pr-7')}
        >
          <option value="V">V</option>
          <option value="E">E</option>
        </select>
        <ChevronDown className="pointer-events-none absolute top-1/2 right-2.5 size-4 -translate-y-1/2 text-slate-400" aria-hidden />
      </div>
      <Input
        id={id}
        inputMode="numeric"
        autoComplete="off"
        placeholder="12345678"
        maxLength={9}
        value={number}
        invalid={invalid}
        aria-describedby={describedBy}
        onBlur={onBlur}
        onChange={(e) => emit(prefix, e.target.value.replace(/\D/g, ''))}
      />
    </div>
  )
}

/** Área de texto con contador de caracteres (el límite asegura que el PDF quepa en 2 hojas). */
export function CountedTextarea({
  value,
  maxLength,
  counterId,
  className,
  ...props
}: ComponentProps<'textarea'> & { value: string; maxLength: number; counterId: string; invalid?: boolean }) {
  const near = value.length >= maxLength * 0.9
  return (
    // El contador va debajo, no dentro: dentro tapaba el asa para agrandar y el texto pasaba por debajo.
    <div>
      <Textarea value={value} maxLength={maxLength} className={className} {...props} />
      <p
        id={counterId}
        className={cn(
          'mt-1 text-right text-xs tabular-nums',
          near ? 'font-semibold text-amber-700 dark:text-amber-400' : 'text-slate-500',
        )}
      >
        {value.length.toLocaleString('es-VE')} / {maxLength.toLocaleString('es-VE')}
      </p>
    </div>
  )
}
