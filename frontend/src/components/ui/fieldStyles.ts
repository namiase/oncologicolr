import { cn } from '@/lib/cn'

export const controlClass = (hasError?: boolean) =>
  cn(
    'block w-full rounded-[10px] border-0 bg-surface px-3 text-sm font-medium text-slate-900 shadow-sm ring-1 ring-inset placeholder:font-normal placeholder:text-slate-500',
    'focus:ring-2 focus:ring-inset focus:outline-none disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500',
    hasError ? 'ring-red-400 focus:ring-red-500' : 'ring-control focus:ring-brand-600',
  )

/** id del mensaje que describe al control (error tiene prioridad sobre la ayuda). */
export const describedBy = (id: string, error?: string, hint?: string) =>
  error ? `${id}-error` : hint ? `${id}-hint` : undefined
