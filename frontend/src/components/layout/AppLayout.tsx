import { Suspense, useCallback, useState } from 'react'
import { Outlet, useLocation } from 'react-router'
import { KeyRound, LogOut, Menu as MenuIcon, X } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Feedback'
import { cn } from '@/lib/cn'
import { ChangePasswordDialog } from '@/features/auth/ChangePasswordDialog'
import { endSession } from '@/features/auth/session'
import { useDocumentTitle } from '@/features/branding/useDocumentTitle'
import { useAuthStore } from '@/stores/authStore'
import type { MenuItem } from '@/types/api'
import { MobileDrawer } from './MobileDrawer'
import { menuPathFor, routeParentOf } from './routeParents'
import { SessionWatcher } from './SessionWatcher'
import { Sidebar } from './Sidebar'
import { ThemeToggle } from './ThemeToggle'

function findTrail(menus: MenuItem[], pathname: string, trail: MenuItem[] = []): MenuItem[] | null {
  for (const menu of menus) {
    const next = [...trail, menu]
    if (menu.path && (pathname === menu.path || pathname.startsWith(`${menu.path}/`))) return next
    const found = findTrail(menu.children, pathname, next)
    if (found) return found
  }
  return null
}

const COLLAPSED_KEY = 'ts:sidebar-collapsed'

const readCollapsed = () => {
  try {
    return localStorage.getItem(COLLAPSED_KEY) === '1'
  } catch {
    return false
  }
}

const initials = (name: string) =>
  name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join('')

export function AppLayout() {
  const [mobileOpen, setMobileOpen] = useState(false)
  // Menú reducido (solo iconos) en escritorio; se recuerda en este navegador.
  const [collapsed, setCollapsed] = useState(readCollapsed)
  const [changingPassword, setChangingPassword] = useState(false)
  const user = useAuthStore((s) => s.user)
  const { pathname } = useLocation()
  const menuTrail = findTrail(user?.menus ?? [], menuPathFor(pathname)) ?? []
  const subpage = menuTrail.length > 0 ? routeParentOf(pathname) : undefined
  const trail = [...menuTrail.map(({ id, title }) => ({ id, title })), ...(subpage ? [{ id: 'subpage', title: subpage.title }] : [])]
  useDocumentTitle(trail.at(-1)?.title)

  // Al cerrar sesión, ProtectedRoute redirige al login automáticamente.
  const logout = () => void endSession()
  const closeMobile = useCallback(() => setMobileOpen(false), [])

  const toggleCollapsed = () => {
    const next = !collapsed
    setCollapsed(next)
    try {
      localStorage.setItem(COLLAPSED_KEY, next ? '1' : '0')
    } catch {
      /* sin almacenamiento: la preferencia dura solo esta visita */
    }
  }

  // Contraseña asignada por un administrador: no se muestra el sistema hasta que la cambie
  // (el API también rechaza todo lo demás mientras tanto).
  if (user?.mustChangePassword) {
    return (
      <div className="min-h-svh">
        <SessionWatcher />
        <ChangePasswordDialog required />
      </div>
    )
  }

  return (
    <div className="min-h-svh">
      {/* Primer elemento al tabular: permite saltar el menú e ir directo al contenido. */}
      <a
        href="#contenido"
        className="sr-only focus:not-sr-only focus:fixed focus:top-3 focus:left-3 focus:z-50 focus:rounded-lg focus:bg-surface focus:px-4 focus:py-2 focus:text-sm focus:font-medium focus:text-brand-700 focus:shadow-lg"
      >
        Saltar al contenido
      </a>
      <SessionWatcher />
      {changingPassword && <ChangePasswordDialog onClose={() => setChangingPassword(false)} />}
      {/* Sidebar fijo en escritorio */}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-30 hidden transition-[width] duration-200 lg:block',
          collapsed ? 'w-18' : 'w-64',
        )}
      >
        <Sidebar collapsed={collapsed} onToggleCollapse={toggleCollapsed} />
      </aside>

      {/* Drawer en móvil */}
      <MobileDrawer open={mobileOpen} onClose={closeMobile} label="Menú">
        <aside className="relative h-full">
          <Sidebar onNavigate={closeMobile} />
          <Button
            variant="ghost"
            size="icon"
            className="absolute right-2 top-2.5 text-sidebar-text hover:bg-white/10 hover:text-white"
            onClick={closeMobile}
            aria-label="Cerrar menú"
          >
            <X className="size-5" />
          </Button>
        </aside>
      </MobileDrawer>

      <div className={cn('transition-[padding] duration-200', collapsed ? 'lg:pl-18' : 'lg:pl-64')}>
        <header className="sticky top-0 z-20 flex h-16 items-center gap-3 border-b border-slate-200 bg-surface/90 px-4 backdrop-blur sm:px-6">
          <Button variant="ghost" size="icon" className="lg:hidden" onClick={() => setMobileOpen(true)} aria-label="Abrir menú">
            <MenuIcon className="size-5" />
          </Button>

          <nav aria-label="Ruta" className="min-w-0 flex-1">
            <ol className="flex items-center gap-2 truncate text-sm">
              {trail.map((item, index) => (
                <li key={item.id} className={index < trail.length - 1 ? "hidden items-center gap-2 truncate sm:flex" : "flex items-center gap-2 truncate"}>
                  {index > 0 && <span className="hidden text-slate-300 sm:inline" aria-hidden>/</span>}
                  <span className={index === trail.length - 1 ? 'font-semibold text-slate-900' : 'font-medium text-slate-500'}>
                    {item.title}
                  </span>
                </li>
              ))}
            </ol>
          </nav>

          {user && (
            <div className="flex items-center gap-1">
              <ThemeToggle />
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setChangingPassword(true)}
                aria-label="Cambiar contraseña"
                title="Cambiar contraseña"
              >
                <KeyRound className="size-5" />
              </Button>
              <div className="ml-2 hidden items-center gap-3 border-l border-slate-200 pl-4 sm:flex">
                <span
                  className="flex size-9 items-center justify-center rounded-full bg-brand-50 text-xs font-bold text-brand-700 dark:bg-brand-500/15 dark:text-brand-300"
                  aria-hidden
                >
                  {initials(user.fullName)}
                </span>
                <div>
                  <p className="text-sm leading-tight font-bold text-slate-900">{user.fullName}</p>
                  <p className="text-xs leading-tight text-slate-500">{user.roleName}</p>
                </div>
              </div>
              <Button variant="ghost" size="icon" onClick={logout} aria-label="Cerrar sesión" title="Cerrar sesión" className="ml-1">
                <LogOut className="size-5" />
              </Button>
            </div>
          )}
        </header>

        <main id="contenido" tabIndex={-1} className="mx-auto w-full max-w-7xl px-4 py-6 focus:outline-none sm:px-6 lg:py-8">
          <Suspense fallback={<Spinner className="p-8" />}>
            <Outlet />
          </Suspense>
        </main>
      </div>
    </div>
  )
}
