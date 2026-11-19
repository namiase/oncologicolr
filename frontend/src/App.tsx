import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router/dom'
import { Toaster } from 'sonner'
import { queryClient } from '@/app/queryClient'
import { router } from '@/app/router'
import { AuthBootstrap } from '@/components/layout/AuthGuards'
import { useTheme } from '@/lib/theme'

// El título de la pestaña lo pone cada pantalla con useDocumentTitle (login y layout).
export function App() {
  const theme = useTheme((s) => s.theme)
  return (
    <QueryClientProvider client={queryClient}>
      <AuthBootstrap>
        <RouterProvider router={router} />
      </AuthBootstrap>
      <Toaster position="top-right" richColors closeButton theme={theme} />
    </QueryClientProvider>
  )
}
