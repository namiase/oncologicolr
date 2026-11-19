import { api } from '@/lib/api'

export const usersApi = {
  resetPassword: async (id: string, newPassword: string) => {
    await api.post(`/users/${id}/reset-password`, { newPassword })
  },
  unlock: async (id: string) => {
    await api.post(`/users/${id}/unlock`)
  },
  /** Solo el administrador principal: autoriza (o no) a un administrador para crear administradores. */
  setAdminDelegation: async (id: string, enabled: boolean) => {
    await api.put(`/users/${id}/admin-delegation`, { enabled })
  },
  changeOwnPassword: async (currentPassword: string, newPassword: string) => {
    await api.post('/auth/change-password', { currentPassword, newPassword })
  },
}
