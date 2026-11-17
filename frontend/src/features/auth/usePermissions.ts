import { useAuthStore } from '@/stores/authStore'
import type { MenuItem } from '@/types/api'

export interface Permissions {
  canView: boolean
  canCreate: boolean
  canUpdate: boolean
  canDelete: boolean
}

const NONE: Permissions = { canView: false, canCreate: false, canUpdate: false, canDelete: false }

function findMenu(menus: MenuItem[], code: string): MenuItem | undefined {
  for (const menu of menus) {
    if (menu.code === code) return menu
    const child = findMenu(menu.children, code)
    if (child) return child
  }
  return undefined
}

/** Permisos del usuario sobre una opción de menú (los mismos que aplica la API). */
export function usePermissions(menuCode: string): Permissions {
  const menus = useAuthStore((s) => s.user?.menus)
  const menu = menus ? findMenu(menus, menuCode) : undefined
  return menu ? { canView: true, ...menu.permissions } : NONE
}
