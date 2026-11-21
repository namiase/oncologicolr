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

// Las listas de opciones usan SearchableSelect (./SearchableSelect) en todo el sistema.
