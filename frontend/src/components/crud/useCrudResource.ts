import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { LookupItem, PagedResult } from '@/types/api'
import type { CrudListParams } from './types'

const cleanParams = ({ extra, ...params }: CrudListParams) =>
  Object.fromEntries(
    Object.entries({ ...params, ...extra }).filter(([, value]) => value !== '' && value !== false),
  )

export function useCrudList<TItem>(basePath: string, params: CrudListParams) {
  return useQuery({
    queryKey: ['crud', basePath, params],
    queryFn: async ({ signal }) =>
      (await api.get<PagedResult<TItem>>(basePath, { params: cleanParams(params), signal })).data,
    placeholderData: keepPreviousData,
  })
}

export function useCrudMutations<TItem>(basePath: string) {
  const queryClient = useQueryClient()
  const invalidate = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: ['crud', basePath] }),
      // Las listas desplegables (p. ej. países en el formulario de estados) también cambian.
      queryClient.invalidateQueries({ queryKey: ['lookup'] }),
      queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
    ])

  const create = useMutation({
    mutationFn: async (payload: unknown) => (await api.post<TItem>(basePath, payload)).data,
    onSuccess: invalidate,
  })

  const update = useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: unknown }) =>
      (await api.put<TItem>(`${basePath}/${id}`, payload)).data,
    onSuccess: invalidate,
  })

  const remove = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`${basePath}/${id}`)
    },
    onSuccess: invalidate,
  })

  return { create, update, remove }
}

export function useLookup(path: string | undefined, parentId?: string) {
  return useQuery({
    queryKey: ['lookup', path, parentId ?? null],
    queryFn: async ({ signal }) =>
      (await api.get<LookupItem[]>(path!, { params: parentId ? { parentId } : undefined, signal })).data,
    enabled: Boolean(path),
    staleTime: 60_000,
  })
}

/** Estatus asignables (Activo / Inactivo). */
export function useStatuses() {
  return useQuery({
    queryKey: ['statuses'],
    queryFn: async () => (await api.get<LookupItem[]>('/statuses')).data,
    staleTime: Infinity,
  })
}
