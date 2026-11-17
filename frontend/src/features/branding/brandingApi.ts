import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'

export interface Branding {
  companyName: string
  logoUrl: string | null
}

export const DEFAULT_LOGO = '/logo-default.svg'

export const brandingApi = {
  get: async () => (await api.get<Branding>('/settings/branding')).data,
  update: async (data: { companyName: string; logo?: File | null; removeLogo?: boolean }) => {
    const form = new FormData()
    form.append('companyName', data.companyName)
    if (data.logo) form.append('logo', data.logo)
    if (data.removeLogo) form.append('removeLogo', 'true')
    return (await api.put<Branding>('/settings/branding', form)).data
  },
}

/** Nombre y logo de la empresa (configuración global, visible incluso sin sesión). */
export function useBranding() {
  return useQuery({ queryKey: ['branding'], queryFn: brandingApi.get, staleTime: 5 * 60_000 })
}
