import type { ReactNode } from 'react'
import { cn } from '@/lib/cn'

interface FieldProps {
  id: string
  label: string
  required?: boolean
  error?: string
  hint?: string
  className?: string
  children: ReactNode
}

/** Etiqueta + control + mensaje de error/ayuda, con los atributos ARIA correctos. */
export function Field({ id, label, required, error, hint, className, children }: FieldProps) {
  return (
    <div className={cn('flex flex-col gap-1.5', className)}>
      <label htmlFor={id} className="text-sm font-medium text-slate-700">
        {label}
        {required && <span className="ml-0.5 text-red-600 dark:text-red-400" aria-hidden>*</span>}
      </label>
      {children}
      {error ? (
        <p id={`${id}-error`} className="text-xs font-medium text-red-600 dark:text-red-400" role="alert">
          {error}
        </p>
      ) : (
        hint && <p id={`${id}-hint`} className="text-xs text-slate-500">{hint}</p>
      )}
    </div>
  )
}

