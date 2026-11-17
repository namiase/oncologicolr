import { createElement } from 'react'
import {
  Building,
  ChartColumn,
  Circle,
  Database,
  FilePlus,
  House,
  Settings,
  UserCog,
  Users,
  type LucideIcon,
} from 'lucide-react'

/** Iconos de menú: el backend envía el nombre (Menu.Icon) y aquí se resuelve el componente. */
const menuIcons: Record<string, LucideIcon> = {
  house: House,
  'file-plus': FilePlus,
  settings: Settings,
  database: Database,
  users: Users,
  'user-cog': UserCog,
  building: Building,
  'building-2': Building,
  'chart-column': ChartColumn,
}

/** Renderiza el icono de un menú por nombre (Circle si no existe). */
export const renderMenuIcon = (name: string | null | undefined, className = 'size-5 shrink-0') =>
  createElement((name && menuIcons[name]) || Circle, { className, 'aria-hidden': true })
