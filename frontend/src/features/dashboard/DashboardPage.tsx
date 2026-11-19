import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { ArrowRight, Database, FilePlus, FileText, UserRound, Users, type LucideIcon } from 'lucide-react'
import { Card } from '@/components/ui/Feedback'
import { usePermissions } from '@/features/auth/usePermissions'
import { api } from '@/lib/api'
import { formatLongDate, formatNumber } from '@/lib/format'
import { useAuthStore } from '@/stores/authStore'
import { MenuCodes } from '@/types/api'

interface DashboardSummary {
  activeSocialWorkers: number
  activeUsers: number
  masterTables: number
  masterTableRecords: number
  reports: number
}

const greeting = (hour: number) => (hour < 12 ? 'Buenos días' : hour < 19 ? 'Buenas tardes' : 'Buenas noches')

interface StatCardProps {
  label: string
  value: string
  icon: LucideIcon
  hint?: string
}

function StatCard({ label, value, icon: Icon, hint }: StatCardProps) {
  return (
    <Card className="p-5">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-medium text-slate-500">{label}</p>
          <p className="mt-2 text-3xl font-semibold tracking-tight text-slate-900 tabular-nums">{value}</p>
          {hint && <p className="mt-1 text-xs text-slate-500">{hint}</p>}
        </div>
        <span className="flex size-10 items-center justify-center rounded-lg bg-brand-50 text-brand-700 dark:bg-brand-500/15 dark:text-brand-300">
          <Icon className="size-5" aria-hidden />
        </span>
      </div>
    </Card>
  )
}

interface QuickLinkProps {
  to: string
  title: string
  description: string
  icon: LucideIcon
}

function QuickLink({ to, title, description, icon: Icon }: QuickLinkProps) {
  return (
    <Link to={to} className="group rounded-xl bg-surface p-5 shadow-sm ring-1 ring-slate-200 transition hover:ring-brand-300 dark:hover:ring-brand-700">
      <Icon className="size-6 text-brand-700 dark:text-brand-400" aria-hidden />
      <p className="mt-3 font-medium text-slate-900">{title}</p>
      <p className="mt-1 text-sm text-slate-500">{description}</p>
      <span className="mt-3 inline-flex items-center gap-1 text-sm font-medium text-brand-700 dark:text-brand-400">
        Ir <ArrowRight className="size-4 transition-transform group-hover:translate-x-0.5" aria-hidden />
      </span>
    </Link>
  )
}

export function DashboardPage() {
  const user = useAuthStore((s) => s.user)
  const summary = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: async () => (await api.get<DashboardSummary>('/dashboard/summary')).data,
  })
  const canCreateReport = usePermissions(MenuCodes.CreateReport).canView
  const canMasterTables = usePermissions(MenuCodes.MasterTables).canView
  const canSocialWorkers = usePermissions(MenuCodes.SocialWorkers).canView

  const [now] = useState(() => new Date())
  const firstName = user?.fullName.split(' ')[0] ?? ''
  const value = (n: number | undefined) => (n === undefined ? '—' : formatNumber(n))

  return (
    <div className="space-y-8">
      <div>
        <p className="text-sm text-slate-500 first-letter:uppercase">{formatLongDate(now)}</p>
        <h1 className="mt-1 text-2xl font-semibold tracking-tight text-slate-900">
          {greeting(now.getHours())}, {firstName}
        </h1>
        <p className="mt-1 text-sm text-slate-500">Este es el resumen general del sistema.</p>
      </div>

      <section aria-label="Métricas generales" className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Trabajadoras sociales activas" value={value(summary.data?.activeSocialWorkers)} icon={Users} />
        <StatCard
          label="Informes elaborados"
          value={value(summary.data?.reports)}
          icon={FileText}
          hint="Disponible con el módulo de informes"
        />
        <StatCard
          label="Registros en tablas maestras"
          value={value(summary.data?.masterTableRecords)}
          icon={Database}
          hint={summary.data ? `${summary.data.masterTables} tablas` : undefined}
        />
        <StatCard label="Usuarios activos" value={value(summary.data?.activeUsers)} icon={UserRound} />
      </section>

      <section aria-labelledby="quick-actions">
        <h2 id="quick-actions" className="text-sm font-semibold text-slate-900">
          Accesos rápidos
        </h2>
        <div className="mt-3 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {canCreateReport && (
            <QuickLink to="/informes/nuevo" title="Crear informe" description="Inicie un nuevo informe social." icon={FilePlus} />
          )}
          {canSocialWorkers && (
            <QuickLink
              to="/configuracion/trabajadoras-sociales"
              title="Trabajadoras sociales"
              description="Registre y actualice el personal."
              icon={Users}
            />
          )}
          {canMasterTables && (
            <QuickLink
              to="/configuracion/tablas-maestras"
              title="Tablas maestras"
              description="Catálogos de sexo, parentesco, ubicación y más."
              icon={Database}
            />
          )}
        </div>
      </section>
    </div>
  )
}
