// Aplica el tema (claro/oscuro) antes de dibujar la página para evitar un destello.
// Es un archivo propio (no un script en línea) para cumplir la Content Security Policy.
// Debe decidir igual que src/lib/theme.ts (misma clave 'ts:theme'): una elección guardada
// ('light' | 'dark') manda; si no hay una válida, se sigue al sistema operativo.
;(function () {
  var stored = null
  try {
    stored = localStorage.getItem('ts:theme')
  } catch {
    /* sin almacenamiento: se sigue al sistema operativo */
  }
  var dark = stored === 'dark' || (stored !== 'light' && window.matchMedia('(prefers-color-scheme: dark)').matches)
  document.documentElement.classList.toggle('dark', dark)
})()
