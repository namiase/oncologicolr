import { useEffect, useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { ImageOff, Save, Upload } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/Button'
import { Card, PageHeader, Spinner } from '@/components/ui/Feedback'
import { Field } from '@/components/ui/Field'
import { Input } from '@/components/ui/Input'
import { usePermissions } from '@/features/auth/usePermissions'
import { getErrorMessage, getFieldErrors } from '@/lib/errors'
import { MenuCodes } from '@/types/api'
import { brandingApi, DEFAULT_LOGO, useBranding, type Branding } from './brandingApi'

const MAX_BYTES = 2 * 1024 * 1024
const ACCEPTED = ['image/png', 'image/jpeg', 'image/webp']

export function CompanySettingsPage() {
  const branding = useBranding()
  if (branding.isPending) return <Spinner className="p-8" />
  if (branding.isError || !branding.data) return <p className="text-sm text-red-600">{getErrorMessage(branding.error)}</p>
  return <CompanySettingsForm initial={branding.data} />
}

function CompanySettingsForm({ initial }: { initial: Branding }) {
  const queryClient = useQueryClient()
  const { canUpdate } = usePermissions(MenuCodes.CompanySettings)
  const [companyName, setCompanyName] = useState(initial.companyName)
  const [selected, setSelected] = useState<{ file: File; url: string } | null>(null)
  const [removeLogo, setRemoveLogo] = useState(false)
  const [errors, setErrors] = useState<Record<string, string>>({})

  // URL local de vista previa: se libera al reemplazarla y al salir de la página.
  const previewUrl = useRef<string | null>(null)
  const selectFile = (file: File | null) => {
    if (previewUrl.current) URL.revokeObjectURL(previewUrl.current)
    const url = file ? URL.createObjectURL(file) : null
    previewUrl.current = url
    setSelected(file && url ? { file, url } : null)
  }
  useEffect(() => () => {
    if (previewUrl.current) URL.revokeObjectURL(previewUrl.current)
  }, [])
  const file = selected?.file ?? null
  const preview = selected?.url ?? null

  const save = useMutation({
    mutationFn: () => brandingApi.update({ companyName: companyName.trim(), logo: file, removeLogo }),
    onSuccess: (data) => {
      queryClient.setQueryData(['branding'], data)
      selectFile(null)
      setRemoveLogo(false)
      setErrors({})
      toast.success('Configuración de la empresa actualizada.')
    },
    onError: (error) => {
      const fieldErrors = getFieldErrors(error)
      setErrors(fieldErrors)
      if (Object.keys(fieldErrors).length === 0) toast.error(getErrorMessage(error))
    },
  })

  const onFileChange = (selected: File | undefined) => {
    if (!selected) return
    if (!ACCEPTED.includes(selected.type)) return setErrors({ logo: 'Formato no permitido. Use PNG, JPG o WEBP.' })
    if (selected.size > MAX_BYTES) return setErrors({ logo: 'El logo debe pesar como máximo 2 MB.' })
    setErrors({})
    setRemoveLogo(false)
    selectFile(selected)
  }

  const shownLogo = preview ?? (removeLogo ? DEFAULT_LOGO : (initial.logoUrl ?? DEFAULT_LOGO))
  const hasCustomLogo = Boolean(preview || (initial.logoUrl && !removeLogo))

  return (
    <>
      <PageHeader title="Empresa y logo" description="El logo se muestra en el menú lateral y en la pantalla de inicio de sesión." />
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
          <Field id="companyName" label="Nombre de la empresa" required error={errors.companyName}>
            <Input
              id="companyName"
              value={companyName}
              maxLength={150}
              disabled={!canUpdate}
              invalid={Boolean(errors.companyName)}
              onChange={(e) => setCompanyName(e.target.value)}
            />
          </Field>

          <div>
            <p className="text-sm font-medium text-slate-700">Logo</p>
            <div className="mt-2 flex flex-col gap-4 sm:flex-row sm:items-center">
              <div className="flex size-28 shrink-0 items-center justify-center rounded-xl bg-slate-50 p-3 ring-1 ring-slate-200">
                <img src={shownLogo} alt="Vista previa del logo" className="max-h-full max-w-full object-contain" />
              </div>
              <div className="space-y-2">
                <div className="flex flex-wrap gap-2">
                  <label
                    className={`inline-flex h-10 cursor-pointer items-center gap-2 rounded-lg bg-surface px-4 text-sm font-medium text-slate-700 shadow-sm ring-1 ring-control ring-inset hover:bg-slate-50 has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-brand-600 ${!canUpdate ? 'pointer-events-none opacity-60' : ''}`}
                  >
                    <Upload className="size-4" aria-hidden />
                    {hasCustomLogo ? 'Cambiar logo' : 'Subir logo'}
                    <input
                      type="file"
                      accept={ACCEPTED.join(',')}
                      className="sr-only"
                      disabled={!canUpdate}
                      onChange={(e) => {
                        onFileChange(e.target.files?.[0])
                        e.target.value = ''
                      }}
                    />
                  </label>
                  {hasCustomLogo && canUpdate && (
                    <Button
                      variant="ghost"
                      icon={<ImageOff className="size-4" />}
                      onClick={() => {
                        selectFile(null)
                        setRemoveLogo(Boolean(initial.logoUrl))
                      }}
                    >
                      Quitar
                    </Button>
                  )}
                </div>
                {errors.logo ? (
                  <p className="text-xs font-medium text-red-600 dark:text-red-400" role="alert">{errors.logo}</p>
                ) : (
                  <p className="text-xs text-slate-500">PNG, JPG o WEBP. Máximo 2 MB. Se recomienda fondo transparente.</p>
                )}
                {file && <p className="text-xs text-slate-600">Archivo seleccionado: {file.name}</p>}
              </div>
            </div>
          </div>

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
