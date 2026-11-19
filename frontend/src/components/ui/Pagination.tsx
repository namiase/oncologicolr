import { ChevronLeft, ChevronRight } from 'lucide-react'
import { formatNumber } from '@/lib/format'
import { Button } from './Button'

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
  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalCount)

  return (
    <div className="flex flex-col gap-3 border-t border-slate-200 px-4 py-3 text-sm text-slate-600 sm:flex-row sm:items-center sm:justify-between">
      <p>
        Mostrando <span className="font-medium text-slate-900">{formatNumber(from)}–{formatNumber(to)}</span> de{' '}
        <span className="font-medium text-slate-900">{formatNumber(totalCount)}</span>
      </p>
      <div className="flex items-center gap-3">
        <label className="flex items-center gap-2">
          <span className="hidden sm:inline">Por página</span>
          <select
            className="h-10 rounded-md border-0 lg:h-8 bg-surface pl-2 pr-7 text-sm ring-1 ring-inset ring-control focus:ring-2 focus:ring-brand-600"
            value={pageSize}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
            aria-label="Registros por página"
          >
            {pageSizes.map((size) => (
              <option key={size} value={size}>{size}</option>
            ))}
          </select>
        </label>
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
