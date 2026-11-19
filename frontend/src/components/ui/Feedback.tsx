import type { ReactNode } from 'react'
import { Inbox, LoaderCircle, type LucideIcon } from 'lucide-react'
import { cn } from '@/lib/cn'

export function Spinner({ className, label = 'Cargando…' }: { className?: string; label?: string }) {
  return (
    <span role="status" className={cn('inline-flex items-center gap-2 text-sm text-slate-500', className)}>
      <LoaderCircle className="size-5 animate-spin text-brand-600" aria-hidden />
      <span>{label}</span>
    </span>
  )
}

export function FullScreenLoader() {
  return (
    <div className="flex min-h-svh items-center justify-center">
      <Spinner label="Cargando sesión…" />
    </div>
  )
}

interface EmptyStateProps {
  icon?: LucideIcon
  title: string
  description?: string
  action?: ReactNode
  className?: string
  /**
   * Etiqueta del título. Dentro de una página que ya tiene su h1 es un párrafo (no abre una
   * sección). Úsese "h1" cuando el estado vacío es toda la página (404, acceso restringido).
   */
  titleAs?: 'h1' | 'p'
}

export function EmptyState({ icon: Icon = Inbox, title, description, action, className, titleAs: Title = 'p' }: EmptyStateProps) {
  return (
    <div className={cn('flex flex-col items-center justify-center px-6 py-14 text-center', className)}>
      <span className="flex size-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
        <Icon className="size-6" aria-hidden />
      </span>
      <Title className="mt-4 text-sm font-semibold text-slate-900">{title}</Title>
      {description && <p className="mt-1 max-w-sm text-sm text-slate-500">{description}</p>}
      {action && <div className="mt-5">{action}</div>}
    </div>
  )
}

export function Card({ className, children }: { className?: string; children: ReactNode }) {
  return <div className={cn('rounded-xl bg-surface shadow-sm ring-1 ring-slate-200', className)}>{children}</div>
}

interface PageHeaderProps {
  title: string
  description?: string
  actions?: ReactNode
}

export function PageHeader({ title, description, actions }: PageHeaderProps) {
  return (
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">{title}</h1>
        {description && <p className="mt-1 text-sm text-slate-500">{description}</p>}
      </div>
      {actions && <div className="flex shrink-0 gap-2">{actions}</div>}
    </div>
  )
}
