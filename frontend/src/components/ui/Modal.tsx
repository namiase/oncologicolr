import { useEffect, useId, useRef, type ReactNode } from 'react'
import { X } from 'lucide-react'
import { cn } from '@/lib/cn'

interface ModalProps {
  open: boolean
  onClose: () => void
  title: string
  description?: string
  children: ReactNode
  footer?: ReactNode
  size?: 'sm' | 'md' | 'lg'
  /** Evita cerrar con Escape o clic fuera (p. ej. mientras se guarda). */
  dismissible?: boolean
}

const widths = { sm: 'max-w-md', md: 'max-w-lg', lg: 'max-w-2xl' }

/** Modal accesible basado en <dialog> nativo (foco atrapado, Escape, backdrop). */
export function Modal({ open, onClose, title, description, children, footer, size = 'md', dismissible = true }: ModalProps) {
  const ref = useRef<HTMLDialogElement>(null)
  const titleId = useId()
  const descriptionId = useId()

  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  return (
    <dialog
      ref={ref}
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onCancel={(e) => {
        e.preventDefault()
        if (dismissible) onClose()
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget && dismissible) onClose()
      }}
      className={cn(
        'm-auto w-[calc(100%-2rem)] rounded-2xl bg-surface p-0 text-slate-900 shadow-2xl backdrop:bg-black/50 backdrop:backdrop-blur-[2px]',
        widths[size],
      )}
    >
      {open && (
        <div className="flex max-h-[85vh] flex-col">
          <header className="flex items-start justify-between gap-4 border-b border-slate-200 px-6 py-4">
            <div>
              <h2 id={titleId} className="text-base font-semibold text-slate-900">{title}</h2>
              {description && <p id={descriptionId} className="mt-1 text-sm text-slate-500">{description}</p>}
            </div>
            <button
              type="button"
              onClick={onClose}
              disabled={!dismissible}
              className="-m-2.5 rounded-md p-2.5 text-slate-500 hover:bg-slate-100 hover:text-slate-700 disabled:opacity-50"
              aria-label="Cerrar"
            >
              <X className="size-5" />
            </button>
          </header>
          <div className="overflow-y-auto px-6 py-5">{children}</div>
          {footer && (
            <footer className="flex flex-col-reverse gap-2 border-t border-slate-200 bg-slate-50 px-6 py-3 sm:flex-row sm:justify-end rounded-b-2xl">
              {footer}
            </footer>
          )}
        </div>
      )}
    </dialog>
  )
}
