import { create } from 'zustand'

export type Theme = 'light' | 'dark'

// La misma clave y la misma regla usa public/theme-init.js, que aplica el tema antes de cargar React.
const STORAGE_KEY = 'ts:theme'
const media = typeof window !== 'undefined' ? window.matchMedia('(prefers-color-scheme: dark)') : null

const readStored = (): Theme | null => {
  try {
    const value = localStorage.getItem(STORAGE_KEY)
    return value === 'light' || value === 'dark' ? value : null
  } catch {
    return null
  }
}

/** Cambia el tema sin animaciones intermedias (las transiciones de color se suspenden un instante). */
const apply = (theme: Theme) => {
  const root = document.documentElement
  root.classList.add('theme-switching')
  root.classList.toggle('dark', theme === 'dark')
  requestAnimationFrame(() => requestAnimationFrame(() => root.classList.remove('theme-switching')))
}

interface ThemeState {
  theme: Theme
  toggle: () => void
}

/**
 * Tema claro/oscuro. Si el usuario no eligió uno, sigue la preferencia del sistema operativo
 * (y cambia con ella). La elección se recuerda en este navegador.
 */
export const useTheme = create<ThemeState>()((set, get) => ({
  theme: document.documentElement.classList.contains('dark') ? 'dark' : 'light',
  toggle: () => {
    const theme: Theme = get().theme === 'dark' ? 'light' : 'dark'
    apply(theme)
    try {
      localStorage.setItem(STORAGE_KEY, theme)
    } catch {
      /* la elección dura solo esta visita */
    }
    set({ theme })
  },
}))

media?.addEventListener('change', (event) => {
  if (readStored()) return
  const theme: Theme = event.matches ? 'dark' : 'light'
  apply(theme)
  useTheme.setState({ theme })
})
