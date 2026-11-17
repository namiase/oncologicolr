import { api } from '@/lib/api'
import type { AuthResponse } from '@/types/api'

export const authApi = {
  login: async (username: string, password: string) =>
    (await api.post<AuthResponse>('/auth/login', { username, password })).data,
}
