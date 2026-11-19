import { useEffect } from 'react'
import { useBranding } from './brandingApi'

/** Título de la pestaña: "Página · Empresa" (o solo la empresa). Ayuda a ubicarse entre pestañas y a los lectores de pantalla. */
export function useDocumentTitle(page?: string) {
  const { data } = useBranding()
  const company = data?.companyName || 'Trabajo Social'
  useEffect(() => {
    document.title = page ? `${page} · ${company}` : company
  }, [page, company])
}
