import { lazy } from 'react'

// Cada módulo se descarga solo al visitarlo (code splitting por ruta).
export const DashboardPage = lazy(() => import('@/features/dashboard/DashboardPage').then((m) => ({ default: m.DashboardPage })))
export const CreateReportPage = lazy(() =>
  import('@/features/reports-create/CreateReportPage').then((m) => ({ default: m.CreateReportPage })),
)
export const EditReportPage = lazy(() =>
  import('@/features/reports-create/EditReportPage').then((m) => ({ default: m.EditReportPage })),
)
export const MasterTablesPage = lazy(() =>
  import('@/features/master-tables/MasterTablesPage').then((m) => ({ default: m.MasterTablesPage })),
)
export const SocialWorkersPage = lazy(() =>
  import('@/features/social-workers/SocialWorkersPage').then((m) => ({ default: m.SocialWorkersPage })),
)
export const CompanySettingsPage = lazy(() =>
  import('@/features/branding/CompanySettingsPage').then((m) => ({ default: m.CompanySettingsPage })),
)
export const ReportsPage = lazy(() => import('@/features/reports/ReportsPage').then((m) => ({ default: m.ReportsPage })))
export const UsersPage = lazy(() => import('@/features/users/UsersPage').then((m) => ({ default: m.UsersPage })))
