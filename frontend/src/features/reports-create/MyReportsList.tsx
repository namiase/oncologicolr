import { useState } from 'react'
import { useNavigate } from 'react-router'
import { FileDown, FileText, Pencil, Search } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Card, EmptyState } from '@/components/ui/Feedback'
import { Input } from '@/components/ui/Input'
import { Pagination } from '@/components/ui/Pagination'
import { cn } from '@/lib/cn'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { useReportList } from './reportsApi'
import { useReportDownload } from './useReportDownload'

const formatDate = (iso: string) => {
  const [y, m, d] = iso.slice(0, 10).split('-')
  return `${d}/${m}/${y}`
}

interface MyReportsListProps {
  canUpdate: boolean
  /** Administrador: ve los informes de todas las trabajadoras. */
  showAll: boolean
}

/** Informes guardados: volver a descargar el PDF o abrirlos para corregirlos. */
export function MyReportsList({ canUpdate, showAll }: MyReportsListProps) {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const debouncedSearch = useDebouncedValue(search.trim(), 350)
  const list = useReportList({ search: debouncedSearch, page, pageSize })
  const pdf = useReportDownload()

  const items = list.data?.items ?? []
  const empty = !list.isPending && items.length === 0

  return (
    <Card>
      <div className="flex flex-col gap-3 border-b border-slate-200 p-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-base font-bold text-slate-900">{showAll ? 'Informes registrados' : 'Mis informes'}</h2>
          <p className="text-sm text-slate-500">
            {showAll ? 'Todos los informes del sistema.' : 'Los informes que usted elaboró o firma.'}
          </p>
        </div>
        <div className="relative sm:w-80">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-slate-400" aria-hidden />
          <Input
            type="search"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setPage(1)
            }}
            placeholder="Buscar por historia, nombre o cédula…"
            aria-label="Buscar informes"
            className="pl-9"
          />
        </div>
      </div>

      {empty ? (
        <EmptyState
          icon={FileText}
          title={debouncedSearch ? 'Sin resultados' : 'Aún no hay informes'}
          description={
            debouncedSearch ? 'Ningún informe coincide con la búsqueda.' : 'Los informes que guarde aparecerán aquí.'
          }
        />
      ) : (
        <div className="overflow-x-auto">
          <table className={cn('min-w-full divide-y divide-slate-200 text-sm', list.isPlaceholderData && 'opacity-60')}>
            <thead className="bg-slate-50">
              <tr>
                {['Historia', 'Paciente', 'Cédula', 'Ingreso', ...(showAll ? ['Trabajadora social'] : [])].map((header, index) => (
                  <th
                    key={header}
                    scope="col"
                    className={cn(
                      'px-4 py-3 text-left text-xs font-semibold tracking-wide text-slate-500 uppercase',
                      index >= 2 && 'hidden md:table-cell',
                    )}
                  >
                    {header}
                  </th>
                ))}
                <th scope="col" className="px-4 py-3">
                  <span className="sr-only">Acciones</span>
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-surface">
              {list.isPending
                ? Array.from({ length: 4 }, (_, row) => (
                    <tr key={row}>
                      <td colSpan={showAll ? 6 : 5} className="px-4 py-3.5">
                        <div className="h-4 w-full max-w-md animate-pulse rounded bg-slate-100" />
                      </td>
                    </tr>
                  ))
                : items.map((report) => (
                    <tr key={report.id} className="hover:bg-hover/70">
                      <td className="px-4 py-3 font-medium whitespace-nowrap text-slate-900 tabular-nums">{report.medicalRecordNumber}</td>
                      <td className="px-4 py-3 text-slate-700">
                        <p className="font-medium text-slate-900">{report.patientName}</p>
                        <p className="mt-0.5 text-xs text-slate-500 md:hidden">
                          {[report.identityCard, formatDate(report.admissionDate)].filter(Boolean).join(' · ')}
                        </p>
                      </td>
                      <td className="hidden px-4 py-3 whitespace-nowrap text-slate-700 tabular-nums md:table-cell">
                        {report.identityCard ?? '—'}
                      </td>
                      <td className="hidden px-4 py-3 whitespace-nowrap text-slate-700 tabular-nums md:table-cell">
                        {formatDate(report.admissionDate)}
                      </td>
                      {showAll && (
                        <td className="hidden px-4 py-3 text-slate-700 md:table-cell">
                          {report.socialWorkerName ?? <span className="text-slate-400">Sin asignar</span>}
                        </td>
                      )}
                      <td className="px-4 py-2 text-right whitespace-nowrap">
                        <div className="inline-flex gap-1">
                          <Button
                            variant="ghost"
                            size="icon"
                            title="Descargar PDF"
                            aria-label={`Descargar PDF: ${report.patientName}`}
                            loading={pdf.downloadingId === report.id}
                            onClick={() => void pdf.download(report)}
                          >
                            {pdf.downloadingId !== report.id && <FileDown className="size-4" />}
                          </Button>
                          {canUpdate && (
                            <Button
                              variant="ghost"
                              size="icon"
                              title="Editar"
                              aria-label={`Editar: ${report.patientName}`}
                              onClick={() => navigate(`/informes/${report.id}/editar`)}
                            >
                              <Pencil className="size-4" />
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
            </tbody>
          </table>
        </div>
      )}

      {list.data && list.data.totalCount > 0 && (
        <Pagination
          page={list.data.page}
          pageSize={list.data.pageSize}
          totalCount={list.data.totalCount}
          totalPages={list.data.totalPages}
          onPageChange={setPage}
          onPageSizeChange={(size) => {
            setPageSize(size)
            setPage(1)
          }}
        />
      )}
      {pdf.dialog}
    </Card>
  )
}
