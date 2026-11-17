import { Moon, Sun } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { useTheme } from '@/lib/theme'

/** Botón para aclarar u oscurecer el sistema. */
export function ThemeToggle({ className }: { className?: string }) {
  const theme = useTheme((s) => s.theme)
  const toggle = useTheme((s) => s.toggle)
  const label = theme === 'dark' ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'

  return (
    <Button variant="ghost" size="icon" onClick={toggle} aria-label={label} title={label} className={className}>
      {theme === 'dark' ? <Sun className="size-5" /> : <Moon className="size-5" />}
    </Button>
  )
}
