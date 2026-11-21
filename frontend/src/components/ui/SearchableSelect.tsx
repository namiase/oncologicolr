import { useCallback, useEffect, useId, useLayoutEffect, useMemo, useRef, useState, type KeyboardEvent, type Ref } from 'react'
import { Check, ChevronDown, Plus, Search } from 'lucide-react'
import { cn } from '@/lib/cn'
import { controlClass } from './fieldStyles'

export interface SearchableOption {
  value: string
  label: string
  /** Texto del campo cerrado si debe ser más corto que la opción (p. ej. solo el código "0414"). */
  short?: string
  /** Encabezado bajo el que se agrupa (opciones consecutivas con el mismo grupo). */
  group?: string
  /** Texto adicional para la búsqueda (no se muestra). */
  keywords?: string
}

/**
 * Opción "Otro" al final de la lista: permite escribir un valor que no está. Si se buscó algo
 * sin resultado exacto, se ofrece como "Agregar «texto»" y onChange recibe el texto escrito.
 */
export interface OtherOption {
  value: string
  label: string
  createLabel?: (query: string) => string
}

interface SearchableSelectProps {
  id: string
  value: string
  options: SearchableOption[]
  /** typed: el texto buscado, cuando se eligió "Agregar «texto»". */
  onChange: (value: string, typed?: string) => void
  onBlur?: () => void
  placeholder?: string
  /** Si se indica, la primera opción deja el campo vacío (p. ej. "Sin indicar"). */
  emptyLabel?: string
  others?: OtherOption[]
  disabled?: boolean
  invalid?: boolean
  loading?: boolean
  describedBy?: string
  /** Nombre accesible si no hay un <label for> (p. ej. en filas de teléfonos). */
  ariaLabel?: string
  /** Se anuncia como obligatorio (el asterisco de la etiqueta es decorativo). */
  required?: boolean
  autoFocus?: boolean
  className?: string
  ref?: Ref<HTMLButtonElement>
}

type Item =
  | { kind: 'empty'; key: string; label: string }
  | { kind: 'option'; key: string; option: SearchableOption }
  | { kind: 'other'; key: string; other: OtherOption; label: string; typed?: string }

/** Sin acentos ni mayúsculas: "Católica" coincide con "catolica". */
const normalize = (text: string) =>
  text
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .trim()

/** Más opciones que estas no se dibujan: se pide afinar la búsqueda. */
const MAX_VISIBLE = 150

/**
 * Listas cortas (Sí/No, sexo…) sin opción "Otro" se abren sin caja de búsqueda: el foco va a la
 * lista y en el celular no se abre el teclado para elegir entre dos opciones.
 */
const SEARCH_MIN_OPTIONS = 8

interface Placement {
  top: number
  left: number
  width: number
  maxHeight: number
  above: boolean
}

/**
 * Lista desplegable con búsqueda integrada (patrón combobox de ARIA): se abre con clic, flechas o
 * escribiendo; se filtra sin importar acentos; flechas + Enter eligen y Escape cierra.
 * El panel usa position: fixed, así no lo recortan los contenedores con scroll (tarjetas, modales).
 */
export function SearchableSelect({
  id,
  value,
  options,
  onChange,
  onBlur,
  placeholder = 'Seleccione…',
  emptyLabel,
  others,
  disabled,
  invalid,
  loading,
  describedBy,
  ariaLabel,
  required,
  autoFocus,
  className,
  ref,
}: SearchableSelectProps) {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [active, setActive] = useState(0)
  const [placement, setPlacement] = useState<Placement | null>(null)
  // Nombre del campo para la búsqueda y la lista (si no, el lector de pantalla solo dice "Buscar").
  const [fieldName, setFieldName] = useState<string | undefined>(ariaLabel)
  const triggerRef = useRef<HTMLButtonElement | null>(null)
  const panelRef = useRef<HTMLDivElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)
  const listRef = useRef<HTMLUListElement>(null)
  const listId = useId()

  const setTrigger = useCallback(
    (element: HTMLButtonElement | null) => {
      triggerRef.current = element
      if (typeof ref === 'function') ref(element)
      else if (ref) ref.current = element
    },
    [ref],
  )

  const searchable = (others?.length ?? 0) > 0 || options.length >= SEARCH_MIN_OPTIONS
  const selectedOption = options.find((o) => o.value === value)
  const selectedOther = others?.find((o) => o.value === value)
  const display = selectedOption ? (selectedOption.short ?? selectedOption.label) : selectedOther?.label

  const { items, hidden } = useMemo(() => {
    const typed = query.trim()
    const tokens = normalize(typed).split(/\s+/).filter(Boolean)
    const matches =
      tokens.length === 0
        ? options
        : options.filter((o) => {
            const text = normalize(`${o.label} ${o.short ?? ''} ${o.keywords ?? ''} ${o.group ?? ''}`)
            return tokens.every((t) => text.includes(t))
          })
    const exact = matches.some((o) => normalize(o.label) === normalize(typed) || normalize(o.short ?? '') === normalize(typed))

    const list: Item[] = []
    if (emptyLabel !== undefined && tokens.length === 0) list.push({ kind: 'empty', key: '__empty__', label: emptyLabel })
    for (const option of matches.slice(0, MAX_VISIBLE)) list.push({ kind: 'option', key: option.value, option })
    for (const other of others ?? []) {
      const create = typed !== '' && !exact && other.createLabel
      list.push({
        kind: 'other',
        key: other.value,
        other,
        label: create ? other.createLabel!(typed) : other.label,
        typed: create ? typed : undefined,
      })
    }
    return { items: list, hidden: Math.max(0, matches.length - MAX_VISIBLE) }
  }, [options, others, query, emptyLabel])

  const place = useCallback(() => {
    const trigger = triggerRef.current
    if (!trigger) return
    const rect = trigger.getBoundingClientRect()
    const below = window.innerHeight - rect.bottom - 8
    const aboveSpace = rect.top - 8
    const above = below < 240 && aboveSpace > below
    const width = Math.min(Math.max(rect.width, 260), window.innerWidth - 16)
    setPlacement({
      top: above ? rect.top - 4 : rect.bottom + 4,
      left: Math.max(8, Math.min(rect.left, window.innerWidth - width - 8)),
      width,
      maxHeight: Math.min(340, Math.max(150, above ? aboveSpace : below)),
      above,
    })
  }, [])

  const close = useCallback(
    (focusTrigger: boolean) => {
      setOpen(false)
      setQuery('')
      onBlur?.()
      if (focusTrigger) triggerRef.current?.focus()
    },
    [onBlur],
  )

  const openList = (initialQuery = '') => {
    if (disabled) return
    place()
    setFieldName(ariaLabel ?? triggerRef.current?.labels?.[0]?.textContent?.replace(/\*\s*$/, '').trim() ?? undefined)
    setQuery(searchable ? initialQuery : '')
    setOpen(true)
  }

  // Al abrir: foco en la búsqueda (o en la lista, si es corta) y la opción elegida activa.
  useEffect(() => {
    if (!open) return
    ;(searchRef.current ?? listRef.current)?.focus()
  }, [open])

  useEffect(() => {
    if (!open) return
    const index = query ? items.findIndex((item) => item.kind === 'option') : items.findIndex((item) => item.key === value)
    setActive(Math.max(0, index))
    // Solo al abrir o al cambiar la búsqueda, no con cada nueva lista.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, query])

  // El panel sigue al campo si se hace scroll o cambia el tamaño de la ventana.
  useLayoutEffect(() => {
    if (!open) return
    const onScroll = (event: Event) => {
      if (panelRef.current?.contains(event.target as Node)) return
      place()
    }
    window.addEventListener('scroll', onScroll, true)
    window.addEventListener('resize', place)
    return () => {
      window.removeEventListener('scroll', onScroll, true)
      window.removeEventListener('resize', place)
    }
  }, [open, place])

  // Clic fuera: cierra sin elegir.
  useEffect(() => {
    if (!open) return
    const onPointerDown = (event: PointerEvent) => {
      const target = event.target as Node
      if (panelRef.current?.contains(target) || triggerRef.current?.contains(target)) return
      close(false)
    }
    document.addEventListener('pointerdown', onPointerDown)
    return () => document.removeEventListener('pointerdown', onPointerDown)
  }, [open, close])

  useEffect(() => {
    if (!open) return
    listRef.current?.querySelector(`[data-index="${active}"]`)?.scrollIntoView({ block: 'nearest' })
  }, [active, open])

  const choose = (item: Item) => {
    if (item.kind === 'empty') onChange('')
    else if (item.kind === 'option') onChange(item.option.value)
    else onChange(item.other.value, item.typed)
    close(true)
  }

  const onTriggerKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    if (['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(event.key)) {
      event.preventDefault()
      openList()
    } else if (event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
      // Escribir sobre el campo cerrado abre la lista ya filtrada.
      event.preventDefault()
      openList(event.key)
    }
  }

  const onListKeyDown = (event: KeyboardEvent<HTMLElement>) => {
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault()
        setActive((i) => Math.min(i + 1, items.length - 1))
        break
      case 'ArrowUp':
        event.preventDefault()
        setActive((i) => Math.max(i - 1, 0))
        break
      case 'Home':
        event.preventDefault()
        setActive(0)
        break
      case 'End':
        event.preventDefault()
        setActive(items.length - 1)
        break
      case 'Enter':
        event.preventDefault()
        if (items[active]) choose(items[active])
        break
      case 'Escape':
        event.preventDefault()
        event.stopPropagation() // no cerrar el modal que lo contiene
        close(true)
        break
      case 'Tab':
        close(false)
        break
    }
  }

  const optionId = (index: number) => `${listId}-${index}`

  return (
    <>
      <button
        ref={setTrigger}
        id={id}
        type="button"
        role="combobox"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={open ? listId : undefined}
        aria-invalid={invalid || undefined}
        aria-describedby={describedBy}
        aria-label={ariaLabel}
        aria-required={required || undefined}
        autoFocus={autoFocus}
        disabled={disabled}
        onClick={() => (open ? close(false) : openList())}
        onKeyDown={onTriggerKeyDown}
        className={cn(controlClass(invalid), 'flex h-10 items-center gap-2 pr-2.5 text-left', className)}
      >
        <span className={cn('min-w-0 flex-1 truncate', !display && 'font-normal text-slate-500')}>
          {display ?? (loading ? 'Cargando…' : placeholder)}
        </span>
        <ChevronDown className={cn('size-4 shrink-0 text-slate-400 transition-transform', open && 'rotate-180')} aria-hidden />
      </button>

      {open && placement && (
        <div
          ref={panelRef}
          className="fixed z-50 flex flex-col overflow-hidden rounded-xl bg-surface shadow-xl ring-1 ring-slate-200"
          style={{
            top: placement.top,
            left: placement.left,
            width: placement.width,
            maxHeight: placement.maxHeight,
            transform: placement.above ? 'translateY(-100%)' : undefined,
          }}
        >
          {searchable && (
            <div className="relative border-b border-slate-200 p-2">
              <Search className="pointer-events-none absolute top-1/2 left-4.5 size-4 -translate-y-1/2 text-slate-400" aria-hidden />
              <input
                ref={searchRef}
                type="text"
                role="combobox"
                aria-expanded
                aria-controls={listId}
                aria-autocomplete="list"
                aria-activedescendant={items[active] ? optionId(active) : undefined}
                aria-label={fieldName ? `Buscar en ${fieldName}` : 'Buscar'}
                autoComplete="off"
                placeholder="Buscar…"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                onKeyDown={onListKeyDown}
                className="h-9 w-full rounded-lg border-0 bg-slate-50 pr-3 pl-8 text-sm text-slate-900 ring-1 ring-control ring-inset placeholder:text-slate-500 focus:ring-2 focus:ring-brand-600 focus:outline-none"
              />
            </div>
          )}

          <ul
            ref={listRef}
            id={listId}
            role="listbox"
            aria-label={fieldName}
            // Lista corta sin búsqueda: la lista recibe el foco y se maneja con las flechas.
            tabIndex={searchable ? undefined : -1}
            aria-activedescendant={!searchable && items[active] ? optionId(active) : undefined}
            onKeyDown={searchable ? undefined : onListKeyDown}
            className="min-h-0 flex-1 overflow-y-auto p-1 focus:outline-none"
          >
            {loading && <li className="px-3 py-2 text-sm text-slate-500">Cargando…</li>}
            {!loading && items.every((item) => item.kind !== 'option') && query.trim() !== '' && (
              <li className="px-3 py-2 text-sm text-slate-500">Sin resultados para «{query.trim()}».</li>
            )}
            {items.map((item, index) => {
              const previous = items[index - 1]
              const group = item.kind === 'option' ? item.option.group : undefined
              const previousGroup = previous?.kind === 'option' ? previous.option.group : undefined
              const selected =
                (item.kind === 'empty' && value === '') || (item.kind !== 'empty' && item.key === value && !(item.kind === 'other' && item.typed))
              return [
                group && group !== previousGroup && (
                  <li
                    key={`group-${group}`}
                    role="presentation"
                    className="px-3 pt-2.5 pb-1 text-xs font-bold tracking-wide text-slate-500 uppercase"
                  >
                    {group}
                  </li>
                ),
                item.kind === 'other' && previous?.kind !== 'other' && index > 0 && (
                  <li key="separator" role="presentation" className="my-1 border-t border-slate-200" />
                ),
                <li
                  key={item.key}
                  id={optionId(index)}
                  data-index={index}
                  role="option"
                  aria-selected={selected}
                  onPointerDown={(e) => e.preventDefault()} // conserva el foco en la búsqueda
                  onClick={() => choose(item)}
                  onPointerMove={() => setActive(index)}
                  className={cn(
                    'flex cursor-pointer items-center gap-2 rounded-lg px-3 py-2 text-sm',
                    index === active ? 'bg-brand-50 text-brand-900 dark:bg-brand-500/15 dark:text-brand-100' : 'text-slate-700',
                    item.kind === 'empty' && 'text-slate-500 italic',
                    item.kind === 'other' && 'font-semibold text-brand-700 dark:text-brand-300',
                  )}
                >
                  {item.kind === 'other' && <Plus className="size-4 shrink-0" aria-hidden />}
                  <span className="min-w-0 flex-1">{item.kind === 'option' ? item.option.label : item.label}</span>
                  {selected && <Check className="size-4 shrink-0 text-brand-700 dark:text-brand-300" aria-hidden />}
                </li>,
              ]
            })}
            {hidden > 0 && (
              <li role="presentation" className="px-3 py-2 text-xs text-slate-500">
                {hidden.toLocaleString('es-VE')} opciones más: escriba para filtrar.
              </li>
            )}
          </ul>
        </div>
      )}
    </>
  )
}
