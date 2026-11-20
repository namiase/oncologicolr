import type { ReactNode } from 'react'
import { cn } from '@/lib/cn'
import { ACTIVE_STATUS_ID } from '@/types/api'

type Tone = 'success' | 'neutral' | 'warning' | 'danger' | 'info'

const tones: Record<Tone, string> = {
  success: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-500/12 dark:text-emerald-300',
  neutral: 'bg-slate-100 text-slate-600',
  warning: 'bg-amber-50 text-amber-800 dark:bg-amber-500/12 dark:text-amber-300',
  danger: 'bg-red-50 text-red-700 dark:bg-red-500/12 dark:text-red-300',
  info: 'bg-brand-50 text-brand-700 dark:bg-brand-500/15 dark:text-brand-300',
}

/** Punto de color de los estados; las etiquetas informativas (p. ej. "Principal") no lo llevan. */
const dots: Record<Tone, string | null> = {
  success: 'bg-emerald-500',
  neutral: 'bg-slate-400',
  warning: 'bg-amber-500',
  danger: 'bg-red-500',
  info: null,
}

export function Badge({ tone = 'neutral', children }: { tone?: Tone; children: ReactNode }) {
  const dot = dots[tone]
  return (
    <span className={cn('inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap', tones[tone])}>
      {dot && <span className={cn('size-1.5 rounded-full', dot)} aria-hidden />}
      {children}
    </span>
  )
}

export function StatusBadge({ statusId, statusName }: { statusId: string; statusName: string }) {
  return <Badge tone={statusId === ACTIVE_STATUS_ID ? 'success' : 'neutral'}>{statusName}</Badge>
}
