import { useEffect } from 'react'
import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router/dom'
import { Toaster } from 'sonner'
import { queryClient } from '@/app/queryClient'
import { router } from '@/app/router'
import { AuthBootstrap } from '@/components/layout/AuthGuards'
import { useBranding } from '@/features/branding/brandingApi'
import { useTheme } from '@/lib/theme'

/** Título de la pestaña con el nombre de la empresa configurado. */
function DocumentTitle() {
  const { data } = useBranding()
  useEffect(() => {
    if (data?.companyName) document.title = data.companyName
  }, [data?.companyName])
  return null
}

export function App() {
  const theme = useTheme((s) => s.theme)
  return (
    <QueryClientProvider client={queryClient}>
      <DocumentTitle />
      <AuthBootstrap>
        <RouterProvider router={router} />
      </AuthBootstrap>
      <Toaster position="top-right" richColors closeButton theme={theme} />
    </QueryClientProvider>
  )
}
