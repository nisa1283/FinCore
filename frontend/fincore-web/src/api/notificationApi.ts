import { notificationClient } from './client'
import type { ApiResponse, AppNotification, PagedResult } from './types'

export const notificationApi = {
    async list(params: { page: number; pageSize: number; unreadOnly: boolean }) {
        const response = await notificationClient.get<ApiResponse<PagedResult<AppNotification>>>('/api/notifications', {
            params,
        })
        return response.data.data
    },

    async getUnreadCount() {
        const response = await notificationClient.get<ApiResponse<{ count: number }>>('/api/notifications/unread-count')
        return response.data.data.count
    },

    async markRead(id: string) {
        await notificationClient.post(`/api/notifications/${id}/read`)
    },

    async markAllRead() {
        await notificationClient.post('/api/notifications/read-all')
    },
}