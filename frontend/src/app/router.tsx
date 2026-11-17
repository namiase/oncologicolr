import type { ReactNode } from 'react'
import { createBrowserRouter, Navigate } from 'react-router'
import { AppLayout } from '@/components/layout/AppLayout'
import { ProtectedRoute, PublicOnlyRoute, RequireMenu } from '@/components/layout/AuthGuards'
import { NotFoundPage } from '@/components/layout/NotFoundPage'
import { LoginPage } from '@/features/auth/LoginPage'
import { MenuCodes, type MenuCode } from '@/types/api'
import {
  CompanySettingsPage,
  CreateReportPage,
  DashboardPage,
  MasterTablesPage,
  ReportsPage,
  SocialWorkersPage,
  UsersPage,
} from './lazyPages'

/** Ruta protegida por permiso de menú (RoleMenu.CanView). */
const guarded = (path: string, code: MenuCode, page: ReactNode) => ({
  path,
  element: <RequireMenu code={code}>{page}</RequireMenu>,
})

export const router = createBrowserRouter([
  {
    path: '/login',
    element: (
      <PublicOnlyRoute>
        <LoginPage />
      </PublicOnlyRoute>
    ),
  },
  {
    element: (
      <ProtectedRoute>
        <AppLayout />
      </ProtectedRoute>
    ),
    children: [
      { index: true, element: <Navigate to="/inicio" replace /> },
      guarded('inicio', MenuCodes.Home, <DashboardPage />),
      guarded('informes/nuevo', MenuCodes.CreateReport, <CreateReportPage />),
      { path: 'configuracion', element: <Navigate to="/configuracion/tablas-maestras" replace /> },
      guarded('configuracion/tablas-maestras/:catalogKey?', MenuCodes.MasterTables, <MasterTablesPage />),
      guarded('configuracion/trabajadoras-sociales', MenuCodes.SocialWorkers, <SocialWorkersPage />),
      guarded('configuracion/usuarios', MenuCodes.Users, <UsersPage />),
      guarded('configuracion/empresa', MenuCodes.CompanySettings, <CompanySettingsPage />),
      guarded('reportes', MenuCodes.Reports, <ReportsPage />),
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
