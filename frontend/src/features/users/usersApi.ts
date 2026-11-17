import { api } from '@/lib/api'

export const usersApi = {
  resetPassword: async (id: string, newPassword: string) => {
    await api.post(`/users/${id}/reset-password`, { newPassword })
  },
  unlock: async (id: string) => {
    await api.post(`/users/${id}/unlock`)
  },
  changeOwnPassword: async (currentPassword: string, newPassword: string) => {
    await api.post('/auth/change-password', { currentPassword, newPassword })
  },
}
