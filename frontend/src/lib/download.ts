/** Descarga un archivo recibido de la API (p. ej. un PDF) con el nombre indicado. */
export function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.rel = 'noopener'
  document.body.append(link)
  link.click()
  link.remove()
  // Se libera después: algunos navegadores leen la URL de forma asíncrona tras el clic.
  setTimeout(() => URL.revokeObjectURL(url), 30_000)
}

/** Nombre de archivo de una cabecera Content-Disposition (prefiere filename* en UTF-8). */
export function fileNameFromDisposition(header: string | undefined, fallback: string): string {
  if (!header) return fallback
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(header)?.[1]
  if (encoded) {
    try {
      return decodeURIComponent(encoded)
    } catch {
      /* nombre mal codificado: se usa el simple */
    }
  }
  return /filename="?([^";]+)"?/i.exec(header)?.[1] ?? fallback
}
