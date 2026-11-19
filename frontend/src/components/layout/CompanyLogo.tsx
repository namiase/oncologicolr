import { useState } from 'react'
import { cn } from '@/lib/cn'
import { DEFAULT_LOGO, useBranding } from '@/features/branding/brandingApi'

interface CompanyLogoProps {
  showName?: boolean
  size?: 'sm' | 'md' | 'lg'
  className?: string
  nameClassName?: string
}

const sizes = { sm: 'h-8 max-w-8', md: 'h-10 max-w-28', lg: 'h-16 max-w-48' }

/** Logo corporativo configurable desde Configuración → Empresa y logo (con logo por defecto). */
export function CompanyLogo({ showName = true, size = 'md', className, nameClassName }: CompanyLogoProps) {
  const { data } = useBranding()
  const [failedUrl, setFailedUrl] = useState<string | null>(null)
  const companyName = data?.companyName ?? 'Trabajo Social'
  const logo = data?.logoUrl && data.logoUrl !== failedUrl ? data.logoUrl : DEFAULT_LOGO

  return (
    <div className={cn('flex min-w-0 items-center gap-3', className)}>
      <img
        src={logo}
        alt={showName ? '' : companyName}
        className={cn('w-auto shrink-0 rounded-md object-contain', sizes[size])}
        onError={() => setFailedUrl(data?.logoUrl ?? null)}
      />
      {showName && <span className={cn('truncate font-semibold text-slate-900', nameClassName)}>{companyName}</span>}
    </div>
  )
}
