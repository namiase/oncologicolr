const dateTime = new Intl.DateTimeFormat('es-VE', { dateStyle: 'medium', timeStyle: 'short' })
const longDate = new Intl.DateTimeFormat('es-VE', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })

export const formatDateTime = (iso: string | null | undefined) => (iso ? dateTime.format(new Date(iso)) : '—')

export const formatLongDate = (date: Date) => longDate.format(date)

export const formatNumber = (value: number) => value.toLocaleString('es-VE')
