import { useWatch, type Control } from 'react-hook-form'
import { Check, CircleDot, TriangleAlert } from 'lucide-react'
import { Card } from '@/components/ui/Feedback'
import { cn } from '@/lib/cn'
import { describeAge, type PhoneRow, type ReportForm } from './reportSchema'
import { REPORT_STEPS, type StepState } from './reportSteps'

interface ReportSummaryProps {
  control: Control<ReportForm>
  states: StepState[]
  onEdit: (step: number) => void
  /** Texto del teléfono de un familiar ("0414-5551234 (Hermano(a))"); '' si la fila está vacía. */
  phoneLabel: (row: PhoneRow) => string
  /** Quién firma; null si se elige al descargar el PDF. */
  signer: string | null
  className?: string
}

const formatDate = (iso: string) => {
  const [y, m, d] = iso.split('-')
  return y && m && d ? `${d}/${m}/${y}` : ''
}

const yesNo = (value: string) => (value === 'true' ? 'sí' : value === 'false' ? 'no' : '—')

const joined = (...parts: (string | null | undefined)[]) => parts.filter(Boolean).join(' · ')

/**
 * "Lo que lleva el informe": lo ya llenado en cada paso, con Editar para volver a ese paso, y lo
 * que falta para poder guardar. Se actualiza mientras se escribe.
 */
export function ReportSummary({ control, states, onEdit, phoneLabel, signer, className }: ReportSummaryProps) {
  const v = useWatch({ control }) as ReportForm

  const filledAreas = [v.medicalSocialArea, v.psychosocialArea, v.socioeconomicArea, v.physicalEnvironmentArea].filter(
    (text) => text?.trim(),
  ).length
  const firstContactPhone = (v.contactPhones ?? []).map(phoneLabel).find(Boolean)

  const blocks: { step: number; rows: [string, string | null | undefined][] }[] = [
    {
      step: 0,
      rows: [
        ['Nombre', [v.lastNames, v.firstNames].filter((part) => part?.trim()).join(', ')],
        ['Cédula', joined(v.identityCard, describeAge(v.birthDate, v.admissionDate))],
        ['Historia', joined(v.medicalRecordNumber, formatDate(v.admissionDate ?? ''))],
      ],
    },
    {
      step: 1,
      rows: [
        ['Seguridad', `IVSS ${yesNo(v.hasIvss)} · IPASME ${yesNo(v.hasIpasme)}`],
        ['Trabajo', v.workplace],
      ],
    },
    {
      step: 2,
      rows: [
        ...(v.isPediatric ? [['Representante', v.guardianFullName] as [string, string]] : []),
        ['Contacto', v.contactFullName],
        ['Teléfono', firstContactPhone],
      ],
    },
    {
      step: 3,
      rows: [
        ['Áreas', `${filledAreas} de 4 con texto`],
        ['Diagnóstico', v.clinicalDiagnosis?.trim() ? 'Escrito' : ''],
        ['Firma', signer ?? 'Se elige al descargar el PDF'],
      ],
    },
  ]

  const missing = [
    !v.medicalRecordNumber?.trim() && 'Historia clínica',
    !v.admissionDate && 'Fecha de ingreso',
    !v.lastNames?.trim() && 'Apellidos',
    !v.firstNames?.trim() && 'Nombres',
    v.isPediatric && !v.guardianFullName?.trim() && 'Representante',
  ].filter(Boolean) as string[]
  const stepsWithErrors = REPORT_STEPS.filter((_, index) => states[index] === 'error').map((step) => step.title)
  const reviewed = states.filter((state) => state === 'done').length

  return (
    <Card className={cn('overflow-hidden', className)}>
      <div className="flex items-baseline justify-between gap-3 border-b border-slate-200 px-4 py-3">
        <h2 className="text-sm font-bold text-slate-900">Lo que lleva el informe</h2>
        <span className="text-xs font-medium text-slate-500">
          {reviewed} de {REPORT_STEPS.length} pasos
        </span>
      </div>
      {blocks.map(({ step, rows }) => {
        const state = states[step]
        return (
          <section
            key={step}
            aria-label={REPORT_STEPS[step]!.title}
            className={cn('border-b border-slate-100 px-4 py-3', state === 'current' && 'bg-brand-50 dark:bg-brand-500/10')}
          >
            <div className="flex items-center justify-between gap-2">
              <h3
                className={cn(
                  'flex items-center gap-1.5 text-[13px] font-bold',
                  state === 'done' && 'text-emerald-700 dark:text-emerald-400',
                  state === 'current' && 'text-brand-800 dark:text-brand-200',
                  state === 'error' && 'text-amber-700 dark:text-amber-400',
                  state === 'todo' && 'text-slate-500',
                )}
              >
                {state === 'done' && <Check className="size-3.5" aria-hidden />}
                {state === 'error' && <TriangleAlert className="size-3.5" aria-hidden />}
                {state === 'current' && <CircleDot className="size-3.5" aria-hidden />}
                {REPORT_STEPS[step]!.title}
                {state === 'current' && <span className="font-medium">(ahora)</span>}
              </h3>
              {state !== 'current' && (
                <button
                  type="button"
                  onClick={() => onEdit(step)}
                  className="-my-2 min-h-11 px-1 text-[13px] font-semibold text-brand-700 hover:text-brand-800 lg:min-h-0 dark:text-brand-300"
                  aria-label={`Editar: ${REPORT_STEPS[step]!.title}`}
                >
                  Editar
                </button>
              )}
            </div>
            {state !== 'todo' && (
              <dl className="mt-1.5 grid grid-cols-[6.5rem_minmax(0,1fr)] gap-x-3 gap-y-0.5 text-[12.5px]">
                {rows.map(([label, value]) => (
                  <div key={label} className="contents">
                    <dt className="text-slate-500">{label}</dt>
                    <dd className={cn('font-semibold wrap-break-word', value ? 'text-slate-700' : 'font-normal text-slate-400')}>
                      {value || '—'}
                    </dd>
                  </div>
                ))}
              </dl>
            )}
          </section>
        )
      })}
      {missing.length > 0 || stepsWithErrors.length > 0 ? (
        <div className="bg-amber-50 px-4 py-3 text-[12.5px] text-amber-800 dark:bg-amber-500/10 dark:text-amber-300">
          <p className="text-[13px] font-bold">Falta antes de guardar</p>
          {missing.length > 0 && <p>{missing.join(' · ')}</p>}
          {stepsWithErrors.length > 0 && <p>Revise: {stepsWithErrors.join(' · ')}</p>}
        </div>
      ) : (
        <div className="flex items-center gap-2 bg-emerald-50 px-4 py-3 text-[13px] font-semibold text-emerald-800 dark:bg-emerald-500/10 dark:text-emerald-300">
          <Check className="size-4" aria-hidden />
          Listo para guardar
        </div>
      )}
    </Card>
  )
}
