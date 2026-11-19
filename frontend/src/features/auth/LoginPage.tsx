import { useState } from 'react'
import { useLocation, useNavigate, type Location } from 'react-router'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Clock, Eye, EyeOff, LogIn } from 'lucide-react'
import { z } from 'zod'
import { Button } from '@/components/ui/Button'
import { Field } from '@/components/ui/Field'
import { describedBy } from '@/components/ui/fieldStyles'
import { Input } from '@/components/ui/Input'
import { CompanyLogo } from '@/components/layout/CompanyLogo'
import { ThemeToggle } from '@/components/layout/ThemeToggle'
import { useBranding } from '@/features/branding/brandingApi'
import { getErrorMessage } from '@/lib/errors'
import { applySession, markActivity } from '@/lib/sessionSync'
import { useAuthStore } from '@/stores/authStore'
import { authApi } from './authApi'

const schema = z.object({
  username: z.string().trim().min(1, 'Ingrese su usuario.'),
  password: z.string().min(1, 'Ingrese su contraseña.'),
})

type LoginForm = z.infer<typeof schema>

const CURRENT_YEAR = new Date().getFullYear()

export function LoginPage() {
  const [showPassword, setShowPassword] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const logoutMessage = useAuthStore((s) => s.logoutMessage)
  const dismissLogoutMessage = useAuthStore((s) => s.dismissLogoutMessage)
  const navigate = useNavigate()
  const location = useLocation()
  const { data: branding } = useBranding()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(schema), defaultValues: { username: '', password: '' } })

  const onSubmit = handleSubmit(async ({ username, password }) => {
    setServerError(null)
    dismissLogoutMessage()
    try {
      const session = await authApi.login(username, password)
      markActivity(true)
      applySession(session, { broadcast: true })
      const from = (location.state as { from?: Location } | null)?.from?.pathname
      navigate(from && from !== '/login' ? from : '/inicio', { replace: true })
    } catch (error) {
      setServerError(getErrorMessage(error, 'No se pudo iniciar sesión.'))
    }
  })

  return (
    <div className="grid min-h-svh lg:grid-cols-2">
      <section className="relative hidden overflow-hidden bg-brand-800 lg:flex lg:flex-col lg:justify-between lg:p-12">
        <CompanyLogo size="md" className="relative" nameClassName="text-lg text-white" />
        <div className="relative max-w-md">
          <h1 className="text-3xl leading-tight font-semibold text-white">
            Atención social integral para pacientes oncológicos.
          </h1>
          <p className="mt-4 text-brand-100">
            Registre la situación social de cada paciente y su grupo familiar, elabore informes y dé seguimiento a cada caso desde un solo lugar.
          </p>
        </div>
        <p className="relative text-sm text-brand-200">
          © {CURRENT_YEAR} {branding?.companyName ?? 'Trabajo Social'}
        </p>
      </section>

      <section className="relative flex items-center justify-center px-4 py-12 sm:px-8">
        <ThemeToggle className="absolute top-4 right-4" />
        <div className="w-full max-w-sm">
          <CompanyLogo size="lg" showName={false} className="mb-3 justify-center lg:hidden" />
          <p className="mb-8 text-center text-sm text-slate-500 lg:hidden">Sistema de trabajo social para pacientes oncológicos</p>
          <h2 className="text-2xl font-semibold tracking-tight text-slate-900">Iniciar sesión</h2>
          <p className="mt-1 text-sm text-slate-500">Ingrese sus credenciales para continuar.</p>

          <form onSubmit={onSubmit} noValidate className="mt-8 space-y-5">
            {logoutMessage && !serverError && (
              <p role="status" className="flex items-start gap-2 rounded-lg bg-amber-50 px-3 py-2.5 text-sm text-amber-800 ring-1 ring-amber-200 dark:bg-amber-500/10 dark:text-amber-200 dark:ring-amber-500/30">
                <Clock className="mt-0.5 size-4 shrink-0" aria-hidden />
                {logoutMessage}
              </p>
            )}
            {serverError && (
              <p role="alert" className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 ring-1 ring-red-200 dark:bg-red-500/10 dark:text-red-300 dark:ring-red-500/30">
                {serverError}
              </p>
            )}

            <Field id="username" label="Usuario" error={errors.username?.message}>
              <Input
                id="username"
                autoComplete="username"
                autoFocus
                invalid={Boolean(errors.username)}
                aria-describedby={describedBy('username', errors.username?.message)}
                {...register('username')}
              />
            </Field>

            <Field id="password" label="Contraseña" error={errors.password?.message}>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  invalid={Boolean(errors.password)}
                  aria-describedby={describedBy('password', errors.password?.message)}
                  className="pr-10"
                  {...register('password')}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 flex items-center px-3 text-slate-500 hover:text-slate-700"
                  aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                >
                  {showPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                </button>
              </div>
            </Field>

            <Button type="submit" className="w-full" loading={isSubmitting} icon={<LogIn className="size-4" />}>
              Ingresar
            </Button>
          </form>
        </div>
      </section>
    </div>
  )
}
