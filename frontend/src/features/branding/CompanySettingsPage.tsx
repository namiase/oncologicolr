import { useEffect, useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { ImageOff, Save, Upload } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/Button'
import { Card, PageHeader, Spinner } from '@/components/ui/Feedback'
import { Field } from '@/components/ui/Field'
import { Input } from '@/components/ui/Input'
import { usePermissions } from '@/features/auth/usePermissions'
import { cn } from '@/lib/cn'
import { getErrorMessage, getFieldErrors } from '@/lib/errors'
import { MenuCodes } from '@/types/api'
import { brandingApi, DEFAULT_LOGO, useBranding, type Branding } from './brandingApi'

const MAX_BYTES = 2 * 1024 * 1024
const LOGO_TYPES = ['image/png', 'image/jpeg', 'image/webp']
/** El membrete se incrusta en el PDF, que no admite WEBP. */
const HEADER_TYPES = ['image/png', 'image/jpeg']

export function CompanySettingsPage() {
  const branding = useBranding()
  if (branding.isPending) return <Spinner className="p-8" />
  if (branding.isError || !branding.data) return <p className="text-sm text-red-600">{getErrorMessage(branding.error)}</p>
  return <CompanySettingsForm initial={branding.data} />
}

/**
 * Imagen elegida y todavía sin guardar, con su URL local de vista previa (se libera al
 * reemplazarla y al salir de la página). "remove" indica que se quitará la imagen guardada.
 */
function useImageChoice() {
  const [selected, setSelected] = useState<{ file: File; url: string } | null>(null)
  const [remove, setRemove] = useState(false)
  const previewUrl = useRef<string | null>(null)

  const select = (file: File | null) => {
    if (previewUrl.current) URL.revokeObjectURL(previewUrl.current)
    const url = file ? URL.createObjectURL(file) : null
    previewUrl.current = url
    setSelected(file && url ? { file, url } : null)
    if (file) setRemove(false)
  }

  useEffect(
    () => () => {
      if (previewUrl.current) URL.revokeObjectURL(previewUrl.current)
    },
    [],
  )

  const reset = () => {
    select(null)
    setRemove(false)
  }

  return { file: selected?.file ?? null, preview: selected?.url ?? null, remove, setRemove, select, reset }
}

type ImageChoice = ReturnType<typeof useImageChoice>

function CompanySettingsForm({ initial }: { initial: Branding }) {
  const queryClient = useQueryClient()
  const { canUpdate } = usePermissions(MenuCodes.CompanySettings)
  const [companyName, setCompanyName] = useState(initial.companyName)
  const logo = useImageChoice()
  const header = useImageChoice()
  const [errors, setErrors] = useState<Record<string, string>>({})

  const save = useMutation({
    mutationFn: () =>
      brandingApi.update({
        companyName: companyName.trim(),
        logo: logo.file,
        removeLogo: logo.remove,
        reportHeader: header.file,
        removeReportHeader: header.remove,
      }),
    onSuccess: (data) => {
      queryClient.setQueryData(['branding'], data)
      logo.reset()
      header.reset()
      setErrors({})
      toast.success('Configuración de la empresa actualizada.')
    },
    onError: (error) => {
      const fieldErrors = getFieldErrors(error)
      setErrors(fieldErrors)
      if (Object.keys(fieldErrors).length === 0) toast.error(getErrorMessage(error))
    },
  })

  const pick = (choice: ImageChoice, field: string, subject: string, types: string[], formats: string) => (file: File | undefined) => {
    if (!file) return
    if (!types.includes(file.type)) return setErrors({ [field]: `Formato no permitido. Use ${formats}.` })
    if (file.size > MAX_BYTES) return setErrors({ [field]: `${subject} debe pesar como máximo 2 MB.` })
    setErrors({})
    choice.select(file)
  }

  return (
    <>
      <PageHeader
        title="Empresa y logo"
        description="El logo se muestra en el menú lateral y en el inicio de sesión. El membrete encabeza los informes impresos."
      />
      <Card className="max-w-3xl">
        <form
          className="space-y-6 p-6"
          noValidate
          onSubmit={(e) => {
            e.preventDefault()
            if (!companyName.trim()) return setErrors({ companyName: 'El nombre de la empresa es obligatorio.' })
            save.mutate()
          }}
        >
          <Field
            id="companyName"
            label="Nombre de la empresa"
            required
            error={errors.companyName}
            hint="También se imprime bajo el membrete de los informes."
          >
            <Input
              id="companyName"
              value={companyName}
              maxLength={150}
              disabled={!canUpdate}
              invalid={Boolean(errors.companyName)}
              aria-describedby={errors.companyName ? 'companyName-error' : 'companyName-hint'}
              onChange={(e) => setCompanyName(e.target.value)}
            />
          </Field>

          <ImagePicker
            label="Logo"
            choice={logo}
            savedUrl={initial.logoUrl}
            fallbackUrl={DEFAULT_LOGO}
            accept={LOGO_TYPES}
            canUpdate={canUpdate}
            error={errors.logo}
            hint="PNG, JPG o WEBP. Máximo 2 MB. Se recomienda fondo transparente."
            previewClassName="size-28"
            onPick={pick(logo, 'logo', 'El logo', LOGO_TYPES, 'PNG, JPG o WEBP')}
          />

          <ImagePicker
            label="Membrete de los informes"
            choice={header}
            savedUrl={initial.reportHeaderUrl}
            accept={HEADER_TYPES}
            canUpdate={canUpdate}
            error={errors.reportHeader}
            hint="Imagen del encabezado oficial (banner del ministerio y logos), a lo ancho. PNG o JPG, máximo 2 MB. Sin membrete, el informe usa el logo y el nombre."
            previewClassName="h-24 w-full sm:w-96"
            onPick={pick(header, 'reportHeader', 'El membrete', HEADER_TYPES, 'PNG o JPG')}
          />

          {canUpdate && (
            <div className="flex justify-end border-t border-slate-200 pt-4">
              <Button type="submit" loading={save.isPending} icon={<Save className="size-4" />}>
                Guardar cambios
              </Button>
            </div>
          )}
        </form>
      </Card>
    </>
  )
}

interface ImagePickerProps {
  label: string
  choice: ImageChoice
  savedUrl: string | null
  /** Imagen que se muestra si no hay ninguna guardada (p. ej. el logo por defecto). */
  fallbackUrl?: string
  accept: string[]
  canUpdate: boolean
  error?: string
  hint: string
  previewClassName: string
  onPick: (file: File | undefined) => void
}

function ImagePicker({ label, choice, savedUrl, fallbackUrl, accept, canUpdate, error, hint, previewClassName, onPick }: ImagePickerProps) {
  const shown = choice.preview ?? (choice.remove ? fallbackUrl : (savedUrl ?? fallbackUrl))
  const hasCustom = Boolean(choice.preview || (savedUrl && !choice.remove))

  return (
    <div>
      <p className="text-sm font-semibold text-slate-800">{label}</p>
      <div className="mt-2 flex flex-col gap-4 sm:flex-row sm:items-center">
        <div
          className={cn(
            'flex shrink-0 items-center justify-center rounded-xl bg-slate-50 p-3 ring-1 ring-slate-200',
            previewClassName,
          )}
        >
          {shown ? (
            <img src={shown} alt={`Vista previa: ${label.toLowerCase()}`} className="max-h-full max-w-full object-contain" />
          ) : (
            <span className="text-xs text-slate-400">Sin imagen</span>
          )}
        </div>
        <div className="space-y-2">
          <div className="flex flex-wrap gap-2">
            <label
              className={cn(
                'inline-flex h-10 cursor-pointer items-center gap-2 rounded-lg bg-surface px-4 text-sm font-medium text-slate-700 shadow-sm ring-1 ring-control ring-inset hover:bg-hover has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-brand-600',
                !canUpdate && 'pointer-events-none opacity-60',
              )}
            >
              <Upload className="size-4" aria-hidden />
              {hasCustom ? 'Cambiar' : 'Subir imagen'}
              <span className="sr-only">: {label}</span>
              <input
                type="file"
                accept={accept.join(',')}
                className="sr-only"
                disabled={!canUpdate}
                onChange={(e) => {
                  onPick(e.target.files?.[0])
                  e.target.value = ''
                }}
              />
            </label>
            {hasCustom && canUpdate && (
              <Button
                variant="ghost"
                icon={<ImageOff className="size-4" />}
                aria-label={`Quitar ${label.toLowerCase()}`}
                onClick={() => {
                  choice.select(null)
                  choice.setRemove(Boolean(savedUrl))
                }}
              >
                Quitar
              </Button>
            )}
          </div>
          {error ? (
            <p className="text-xs font-medium text-red-600 dark:text-red-400" role="alert">
              {error}
            </p>
          ) : (
            <p className="max-w-md text-xs text-slate-500">{hint}</p>
          )}
          {choice.file && <p className="text-xs text-slate-600">Archivo seleccionado: {choice.file.name}</p>}
        </div>
      </div>
    </div>
  )
}
