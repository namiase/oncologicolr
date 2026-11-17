import type { ReactNode } from 'react'
import { cn } from '@/lib/cn'
import { ACTIVE_STATUS_ID } from '@/types/api'

type Tone = 'success' | 'neutral' | 'danger' | 'info'

const tones: Record<Tone, string> = {
  success: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20 dark:bg-emerald-500/10 dark:text-emerald-300 dark:ring-emerald-500/30',
  neutral: 'bg-slate-100 text-slate-600 ring-slate-500/20',
  danger: 'bg-red-50 text-red-700 ring-red-600/20 dark:bg-red-500/10 dark:text-red-300 dark:ring-red-500/30',
  info: 'bg-brand-50 text-brand-700 ring-brand-600/20 dark:bg-brand-500/15 dark:text-brand-300 dark:ring-brand-500/30',
}

export function Badge({ tone = 'neutral', children }: { tone?: Tone; children: ReactNode }) {
  return (
    <span className={cn('inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset', tones[tone])}>
      {children}
    </span>
  )
}



export function StatusBadge({ statusId, statusName }: { statusId: string; statusName: string }) {
  const active = statusId === ACTIVE_STATUS_ID
  return (
    <Badge tone={active ? 'success' : 'neutral'}>
      <span className={cn('size-1.5 rounded-full', active ? 'bg-emerald-500' : 'bg-slate-400')} aria-hidden />
      {statusName}
    </Badge>
  )
}
