import { Check, TriangleAlert } from 'lucide-react'
import { cn } from '@/lib/cn'
import { REPORT_STEPS, type StepState } from './reportSteps'

interface ReportProgressProps {
  states: StepState[]
  onSelect: (step: number) => void
}

/** Barra de avance por tramos con el nombre de cada paso; cada tramo lleva a su paso. */
export function ReportProgress({ states, onSelect }: ReportProgressProps) {
  const current = states.indexOf('current')
  return (
    <nav aria-label="Pasos del informe">
      <p className="mb-2 text-sm font-semibold text-slate-900 sm:hidden">
        Paso {current + 1} de {REPORT_STEPS.length} · {REPORT_STEPS[current]?.title}
      </p>
      <ol className="grid grid-cols-4 gap-1.5">
        {REPORT_STEPS.map((step, index) => {
          const state = states[index]
          return (
            <li key={step.title}>
              <button
                type="button"
                onClick={() => onSelect(index)}
                aria-current={state === 'current' ? 'step' : undefined}
                aria-label={`Paso ${index + 1}: ${step.title}${state === 'done' ? ' (revisado)' : state === 'error' ? ' (tiene errores)' : ''}`}
                className="group flex min-h-11 w-full flex-col justify-center gap-2 rounded-md text-left sm:min-h-0"
              >
                <span className="block h-1.5 overflow-hidden rounded-full bg-slate-200">
                  <span
                    className={cn(
                      'block h-full rounded-full transition-[width] duration-200',
                      state === 'done' && 'w-full bg-brand-600',
                      state === 'current' && 'w-1/2 bg-brand-600',
                      state === 'error' && 'w-full bg-amber-500',
                      state === 'todo' && 'w-0',
                    )}
                  />
                </span>
                <span
                  className={cn(
                    'hidden items-center gap-1.5 text-[13px] leading-tight sm:flex',
                    state === 'done' && 'font-semibold text-emerald-700 dark:text-emerald-400',
                    state === 'current' && 'font-bold text-slate-900',
                    state === 'error' && 'font-semibold text-amber-700 dark:text-amber-400',
                    state === 'todo' && 'font-medium text-slate-500 group-hover:text-slate-700',
                  )}
                >
                  {state === 'done' ? (
                    <Check className="size-3.5 shrink-0" aria-hidden />
                  ) : state === 'error' ? (
                    <TriangleAlert className="size-3.5 shrink-0" aria-hidden />
                  ) : (
                    <span className="tabular-nums">{index + 1}</span>
                  )}
                  <span className="truncate">{step.title}</span>
                </span>
              </button>
            </li>
          )
        })}
      </ol>
    </nav>
  )
}
