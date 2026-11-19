import { useEffect, useRef, type ReactNode } from 'react'

interface MobileDrawerProps {
  open: boolean
  onClose: () => void
  label: string
  children: ReactNode
}

const DESKTOP = '(min-width: 1024px)'

/**
 * Menú lateral en móvil sobre <dialog> nativo: el foco entra al abrir y queda atrapado,
 * Escape lo cierra, el fondo queda inerte y, al cerrar, el foco vuelve al botón que lo abrió.
 */
export function MobileDrawer({ open, onClose, label, children }: MobileDrawerProps) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  // Si la ventana pasa a escritorio con el menú abierto, se cierra (si no, la página quedaría inerte).
  useEffect(() => {
    if (!open) return
    const media = window.matchMedia(DESKTOP)
    const onChange = () => media.matches && onClose()
    onChange()
    media.addEventListener('change', onChange)
    return () => media.removeEventListener('change', onChange)
  }, [open, onClose])

  return (
    <dialog
      ref={ref}
      aria-label={label}
      onCancel={(e) => {
        e.preventDefault()
        onClose()
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose()
      }}
      className="m-0 h-dvh max-h-none w-72 max-w-[85vw] bg-transparent p-0 text-slate-900 shadow-xl backdrop:bg-black/50"
    >
      {open && children}
    </dialog>
  )
}
