import { NavLink, useLocation } from 'react-router'
import { PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import { cn } from '@/lib/cn'
import { renderMenuIcon } from '@/lib/icons'
import { useAuthStore } from '@/stores/authStore'
import type { MenuItem } from '@/types/api'
import { CompanyLogo } from './CompanyLogo'
import { routeParentOf } from './routeParents'

/** Título de la sección que agrupa las opciones sueltas del módulo (Inicio, Crear informe, Reportes). */
const MODULE_SECTION = 'Trabajo social'

interface Section {
  id: string
  title: string
  items: MenuItem[]
}

/**
 * Las opciones sueltas forman la sección del módulo; cada grupo del backend (p. ej. Configuración)
 * es otra sección siempre visible. Así un módulo nuevo se agrega como una sección más.
 */
const toSections = (menus: MenuItem[]): Section[] =>
  [
    { id: 'module', title: MODULE_SECTION, items: menus.filter((m) => m.children.length === 0) },
    ...menus.filter((m) => m.children.length > 0).map((m) => ({ id: m.id, title: m.title, items: m.children })),
  ].filter((s) => s.items.length > 0)

const itemClass = (active: boolean, collapsed: boolean) =>
  cn(
    'relative flex items-center gap-3 rounded-lg py-3 text-sm font-medium transition-colors lg:py-2',
    collapsed ? 'justify-center px-0' : 'px-3',
    active
      ? cn(
          'bg-teal-400/12 font-semibold text-teal-300',
          // Barra en el borde del menú que marca la opción actual.
          'before:absolute before:inset-y-2 before:w-[3px] before:rounded-r before:bg-teal-400',
          collapsed ? 'before:-left-2' : 'before:-left-3',
        )
      : 'text-sidebar-text hover:bg-white/5 hover:text-white',
  )

interface SidebarProps {
  /** Menú reducido: solo iconos (escritorio). */
  collapsed?: boolean
  /** Si se indica, muestra el botón para reducir/expandir el menú. */
  onToggleCollapse?: () => void
  onNavigate?: () => void
}

export function Sidebar({ collapsed = false, onToggleCollapse, onNavigate }: SidebarProps) {
  const menus = useAuthStore((s) => s.user?.menus ?? [])
  const sections = toSections(menus)
  // Pantallas sin opción propia (p. ej. Editar informe) marcan la opción a la que pertenecen.
  const parentPath = routeParentOf(useLocation().pathname)?.parent

  return (
    <div className="flex h-full flex-col bg-sidebar">
      <div className={cn('flex h-16 shrink-0 items-center border-b border-sidebar-line', collapsed ? 'justify-center' : 'px-5')}>
        <CompanyLogo size="sm" framed showName={!collapsed} nameClassName="text-sm text-white" />
      </div>

      <nav aria-label="Menú principal" className={cn('flex-1 overflow-y-auto py-3', collapsed ? 'px-2' : 'px-3')}>
        {sections.map((section, index) => (
          <div key={section.id} className={cn(index > 0 && (collapsed ? 'mt-3 border-t border-sidebar-line pt-3' : 'mt-4'))}>
            {collapsed ? (
              <p className="sr-only">{section.title}</p>
            ) : (
              <p className="px-3 pt-1 pb-2 text-[11px] font-bold tracking-[0.08em] text-sidebar-muted uppercase">{section.title}</p>
            )}
            <ul className="space-y-0.5">
              {section.items.map((item) => (
                <li key={item.id}>
                  <NavLink
                    to={item.path ?? '#'}
                    title={collapsed ? item.title : undefined}
                    className={({ isActive }) => itemClass(isActive || item.path === parentPath, collapsed)}
                    onClick={onNavigate}
                  >
                    {renderMenuIcon(item.icon)}
                    <span className={collapsed ? 'sr-only' : undefined}>{item.title}</span>
                  </NavLink>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </nav>

      <div className={cn('border-t border-sidebar-line py-2', collapsed ? 'px-2' : 'px-3')}>
        {onToggleCollapse ? (
          <button
            type="button"
            onClick={onToggleCollapse}
            title={collapsed ? 'Expandir menú' : 'Reducir menú'}
            aria-label={collapsed ? 'Expandir menú' : 'Reducir menú'}
            className={cn(itemClass(false, collapsed), 'w-full text-sidebar-muted')}
          >
            {collapsed ? (
              <PanelLeftOpen className="size-5 shrink-0" aria-hidden />
            ) : (
              <>
                <PanelLeftClose className="size-5 shrink-0" aria-hidden />
                <span className="flex-1 text-left">Reducir menú</span>
                <span className="text-xs">v1.0</span>
              </>
            )}
          </button>
        ) : (
          <p className="px-2 py-1 text-xs text-sidebar-muted">Versión 1.0</p>
        )}
      </div>
    </div>
  )
}
