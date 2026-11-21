/**
 * Pantallas sin opción de menú propia: se muestran dentro de la opción indicada (`parent`), que
 * queda marcada en el menú, y suman `title` al final de la ruta de la barra superior.
 */
interface RouteParent {
  pattern: RegExp
  parent: string
  title: string
}

const ROUTE_PARENTS: RouteParent[] = [{ pattern: /^\/informes\/[^/]+\/editar$/, parent: '/informes/nuevo', title: 'Editar informe' }]

export const routeParentOf = (pathname: string) => ROUTE_PARENTS.find((route) => route.pattern.test(pathname))

/** Ruta de la opción de menú a la que pertenece la pantalla actual. */
export const menuPathFor = (pathname: string) => routeParentOf(pathname)?.parent ?? pathname
