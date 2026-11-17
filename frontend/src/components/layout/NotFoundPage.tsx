import { Link } from 'react-router'
import { SearchX } from 'lucide-react'
import { EmptyState } from '@/components/ui/Feedback'

export function NotFoundPage() {
  return (
    <EmptyState
      icon={SearchX}
      title="Página no encontrada"
      description="La dirección no existe o fue movida."
      action={
        <Link to="/inicio" className="text-sm font-medium text-brand-700 hover:underline dark:text-brand-400">
          Volver al inicio
        </Link>
      }
    />
  )
}
