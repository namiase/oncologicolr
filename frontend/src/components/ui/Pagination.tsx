import { useId } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { formatNumber } from '@/lib/format'
import { Button } from './Button'
import { SearchableSelect } from './SearchableSelect'

interface PaginationProps {
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  onPageChange: (page: number) => void
  onPageSizeChange: (size: number) => void
  pageSizes?: number[]
}

export function Pagination({
  page,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
  onPageSizeChange,
  pageSizes = [10, 25, 50],
}: PaginationProps) {
  const sizeId = useId()
  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalCount)

  return (
    <div className="flex flex-col gap-3 border-t border-slate-200 px-4 py-3 text-sm text-slate-600 sm:flex-row sm:items-center sm:justify-between">
      <p>
        Mostrando <span className="font-medium text-slate-900">{formatNumber(from)}–{formatNumber(to)}</span> de{' '}
        <span className="font-medium text-slate-900">{formatNumber(totalCount)}</span>
      </p>
      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2">
          <span className="hidden sm:inline" aria-hidden>
            Por página
          </span>
          <SearchableSelect
            id={sizeId}
            className="w-20"
            ariaLabel="Registros por página"
            value={String(pageSize)}
            onChange={(size) => onPageSizeChange(Number(size))}
            options={pageSizes.map((size) => ({ value: String(size), label: String(size) }))}
          />
        </div>
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="icon" onClick={() => onPageChange(page - 1)} disabled={page <= 1} aria-label="Página anterior">
            <ChevronLeft className="size-4" />
          </Button>
          <span className="min-w-16 text-center tabular-nums">
            {page} / {Math.max(totalPages, 1)}
          </span>
          <Button variant="ghost" size="icon" onClick={() => onPageChange(page + 1)} disabled={page >= totalPages} aria-label="Página siguiente">
            <ChevronRight className="size-4" />
          </Button>
        </div>
      </div>
    </div>
  )
}
