import { useId } from 'react'
import { get, useForm, type FieldError, type FieldValues, type Path, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button } from '@/components/ui/Button'
import { Modal } from '@/components/ui/Modal'
import { getErrorMessage, getFieldErrors } from '@/lib/errors'
import { DynamicField } from './DynamicField'
import { crudTexts, type CrudEntity, type CrudResource, type FormMode } from './types'

interface CrudFormModalProps<TItem extends CrudEntity, TForm extends FieldValues> {
  resource: CrudResource<TItem, TForm>
  /** null = crear; un registro = editar. */
  item: TItem | null
  onClose: () => void
  onSubmit: (form: TForm) => Promise<void>
}

/**
 * Formulario de alta/edición generado a partir de resource.fields. Se monta al abrir,
 * así que cada apertura empieza con valores limpios. Los errores 400/409 de la API se
 * muestran sobre el campo correspondiente.
 */
export function CrudFormModal<TItem extends CrudEntity, TForm extends FieldValues>({
  resource,
  item,
  onClose,
  onSubmit,
}: CrudFormModalProps<TItem, TForm>) {
  const idPrefix = useId()
  const mode: FormMode = item ? 'edit' : 'create'
  const fields = resource.fields.filter((f) => !f.only || f.only === mode)
  const fieldNames = new Set<string>(fields.map((f) => f.name))
  const schema = mode === 'create' ? (resource.createSchema ?? resource.schema) : resource.schema

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<TForm>({
    resolver: zodResolver(schema) as unknown as Resolver<TForm>,
    defaultValues: (item ? resource.toForm(item) : resource.emptyForm) as never,
  })

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(values)
    } catch (error) {
      const fieldErrors = getFieldErrors(error)
      const matched = Object.entries(fieldErrors).filter(([name]) => fieldNames.has(name))
      matched.forEach(([name, message], index) =>
        setError(name as Path<TForm>, { type: 'server', message }, { shouldFocus: index === 0 }),
      )
      if (matched.length === 0) setError('root.server', { type: 'server', message: getErrorMessage(error) })
    }
  })

  const texts = crudTexts(resource.singular, resource.feminine)
  const title = item ? texts.editLabel : texts.newLabel
  const formId = `${idPrefix}-form`
  const rootError = errors.root?.server?.message

  return (
    <Modal
      open
      onClose={onClose}
      title={title}
      description={resource.title}
      size="lg"
      dismissible={!isSubmitting}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>Cancelar</Button>
          <Button type="submit" form={formId} loading={isSubmitting}>{item ? 'Guardar cambios' : 'Crear'}</Button>
        </>
      }
    >
      <form id={formId} onSubmit={submit} noValidate className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        {rootError && (
          <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700 ring-1 ring-red-200 dark:bg-red-500/10 dark:text-red-300 dark:ring-red-500/30 sm:col-span-2">
            {rootError}
          </p>
        )}
        {fields.map((field, index) => (
          <DynamicField<TForm, TItem>
            key={field.name}
            field={field}
            item={item}
            mode={mode}
            idPrefix={idPrefix}
            register={register}
            control={control}
            error={get(errors, field.name) as FieldError | undefined}
            autoFocus={index === 0 && !(mode === 'edit' && field.readOnlyOnEdit)}
          />
        ))}
      </form>
    </Modal>
  )
}
