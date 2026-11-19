import { useEffect, useId, useRef, useState } from 'react'
import { NavLink, useLocation } from 'react-router'
import { ChevronDown, PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import { cn } from '@/lib/cn'
import { renderMenuIcon } from '@/lib/icons'
import { useAuthStore } from '@/stores/authStore'
import type { MenuItem } from '@/types/api'
import { CompanyLogo } from './CompanyLogo'

const isActivePath = (menu: MenuItem, pathname: string): boolean =>
  (menu.path !== null && (pathname === menu.path || pathname.startsWith(`${menu.path}/`))) ||
  menu.children.some((child) => isActivePath(child, pathname))

const itemClass = (active: boolean, collapsed: boolean) =>
  cn(
    'flex items-center gap-3 rounded-lg py-2 text-sm font-medium transition-colors',
    collapsed ? 'justify-center px-0' : 'px-3',
    active ? 'bg-brand-50 text-brand-800 dark:bg-brand-500/15 dark:text-brand-300' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900',
  )

/** Grupo con submenú en el menú expandido (se despliega en el lugar). */
function MenuGroup({ menu, onNavigate }: { menu: MenuItem; onNavigate?: () => void }) {
  const { pathname } = useLocation()
  const active = isActivePath(menu, pathname)
  const [open, setOpen] = useState(active)
  const expanded = open || active

  return (
    <li>
      <button
        type="button"
        onClick={() => setOpen(!expanded)}
        aria-expanded={expanded}
        className={cn(itemClass(false, false), 'w-full', active && 'text-brand-800 dark:text-brand-300')}
      >
        {renderMenuIcon(menu.icon)}
        <span className="flex-1 text-left">{menu.title}</span>
        <ChevronDown className={cn('size-4 transition-transform', expanded && 'rotate-180')} aria-hidden />
      </button>
      {expanded && (
        <ul className="mt-1 ml-5 space-y-1 border-l border-slate-200 pl-3">
          {menu.children.map((child) => (
            <li key={child.id}>
              <NavLink to={child.path ?? '#'} className={({ isActive }) => itemClass(isActive, false)} onClick={onNavigate}>
                {child.title}
              </NavLink>
            </li>
          ))}
        </ul>
      )}
    </li>
  )
}

/** Grupo con submenú en el menú reducido: el submenú se abre como panel flotante al lado. */
function CollapsedMenuGroup({ menu }: { menu: MenuItem }) {
  const { pathname } = useLocation()
  const active = isActivePath(menu, pathname)
  const [position, setPosition] = useState<{ top: number; left: number } | null>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)
  const panelRef = useRef<HTMLDivElement>(null)
  const panelId = useId()

  useEffect(() => {
    if (!position) return
    const close = () => setPosition(null)
    const onPointerDown = (event: PointerEvent) => {
      const target = event.target as Node
      if (!panelRef.current?.contains(target) && !buttonRef.current?.contains(target)) close()
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      close()
      buttonRef.current?.focus()
    }
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    document.addEventListener('scroll', close, true)
    window.addEventListener('resize', close)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
      document.removeEventListener('scroll', close, true)
      window.removeEventListener('resize', close)
    }
  }, [position])

  const toggle = () => {
    if (position) return setPosition(null)
    const rect = buttonRef.current?.getBoundingClientRect()
    if (rect) setPosition({ top: rect.top, left: rect.right + 8 })
  }

  return (
    <li>
      <button
        ref={buttonRef}
        type="button"
        onClick={toggle}
        title={menu.title}
        aria-expanded={position !== null}
        aria-controls={position ? panelId : undefined}
        className={cn(itemClass(active, true), 'w-full')}
      >
        {renderMenuIcon(menu.icon)}
        <span className="sr-only">{menu.title}</span>
      </button>
      {position && (
        <div
          id={panelId}
          ref={panelRef}
          style={{ top: position.top, left: position.left }}
          className="fixed z-50 w-56 rounded-xl bg-surface p-2 shadow-lg ring-1 ring-slate-200"
        >
          <p className="px-3 pt-1 pb-2 text-xs font-semibold tracking-wide text-slate-500 uppercase">{menu.title}</p>
          <ul className="space-y-1">
            {menu.children.map((child) => (
              <li key={child.id}>
                <NavLink
                  to={child.path ?? '#'}
                  className={({ isActive }) => itemClass(isActive, false)}
                  onClick={() => setPosition(null)}
                >
                  {child.title}
                </NavLink>
              </li>
            ))}
          </ul>
        </div>
      )}
    </li>
  )
}

interface SidebarProps {
  /** Menú reducido: solo iconos (escritorio). */
  collapsed?: boolean
  /** Si se indica, muestra el botón para reducir/expandir el menú. */
  onToggleCollapse?: () => void
  onNavigate?: () => void
}

export function Sidebar({ collapsed = false, onToggleCollapse, onNavigate }: SidebarProps) {
  const menus = useAuthStore((s) => s.user?.menus ?? [])

  return (
    <div className="flex h-full flex-col bg-surface">
      <div className={cn('flex h-16 shrink-0 items-center border-b border-slate-200', collapsed ? 'justify-center' : 'px-5')}>
        <CompanyLogo size="sm" showName={!collapsed} nameClassName="text-sm" />
      </div>

      <nav aria-label="Menú principal" className={cn('flex-1 overflow-y-auto py-4', collapsed ? 'px-2' : 'px-3')}>
        <ul className="space-y-1">
          {menus.map((menu) => {
            if (menu.children.length > 0) {
              return collapsed ? (
                <CollapsedMenuGroup key={menu.id} menu={menu} />
              ) : (
                <MenuGroup key={menu.id} menu={menu} onNavigate={onNavigate} />
              )
            }
            return (
              <li key={menu.id}>
                <NavLink
                  to={menu.path ?? '#'}
                  title={collapsed ? menu.title : undefined}
                  className={({ isActive }) => itemClass(isActive, collapsed)}
                  onClick={onNavigate}
                >
                  {renderMenuIcon(menu.icon)}
                  <span className={collapsed ? 'sr-only' : undefined}>{menu.title}</span>
                </NavLink>
              </li>
            )
          })}
        </ul>
      </nav>

      <div className={cn('border-t border-slate-200 py-2', collapsed ? 'px-2' : 'px-3')}>
        {onToggleCollapse ? (
          <button
            type="button"
            onClick={onToggleCollapse}
            title={collapsed ? 'Expandir menú' : 'Reducir menú'}
            aria-label={collapsed ? 'Expandir menú' : 'Reducir menú'}
            className={cn(itemClass(false, collapsed), 'w-full text-slate-500')}
          >
            {collapsed ? (
              <PanelLeftOpen className="size-5 shrink-0" aria-hidden />
            ) : (
              <>
                <PanelLeftClose className="size-5 shrink-0" aria-hidden />
                <span className="flex-1 text-left">Reducir menú</span>
                <span className="text-xs text-slate-500">v1.0</span>
              </>
            )}
          </button>
        ) : (
          <p className="px-2 py-1 text-xs text-slate-500">Versión 1.0</p>
        )}
      </div>
    </div>
  )
}
