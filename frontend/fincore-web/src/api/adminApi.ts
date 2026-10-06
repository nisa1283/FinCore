import { accountClient, authClient, transactionClient } from './client'
import type {
    AdminAccountItem,
    AdminTransactionItem,
    AdminUserItem,
    ApiResponse,
    PagedResult,
    TransactionFilters,
    TransactionStats,
} from './types'

// Do not send empty filters to the API
function clean(params: object) {
    return Object.fromEntries(Object.entries(params).filter(([, value]) => value !== '' && value !== undefined))
}

export interface AdminUserFilters {
    page: number
    pageSize: number
    search?: string
    isActive?: string
}

export interface AdminAccountFilters {
    page: number
    pageSize: number
    search?: string
    status?: string
    userId?: string
}

export const adminApi = {
    async listUsers(filters: AdminUserFilters) {
        const response = await authClient.get<ApiResponse<PagedResult<AdminUserItem>>>('/api/admin/users', {
            params: clean(filters),
        })
        return response.data.data
    },

    async setUserActive(id: string, active: boolean) {
        const response = await authClient.post<ApiResponse<AdminUserItem>>(
            `/api/admin/users/${id}/${active ? 'activate' : 'deactivate'}`,
        )
        return response.data.data
    },

    async unlockUser(id: string) {
        const response = await authClient.post<ApiResponse<AdminUserItem>>(`/api/admin/users/${id}/unlock`)
        return response.data.data
    },

    async listAccounts(filters: AdminAccountFilters) {
        const response = await accountClient.get<ApiResponse<PagedResult<AdminAccountItem>>>('/api/admin/accounts', {
            params: clean(filters),
        })
        return response.data.data
    },

    // Admins are allowed to freeze/unfreeze any account on the regular endpoints
    async setAccountFrozen(id: string, frozen: boolean) {
        await accountClient.post(`/api/accounts/${id}/${frozen ? 'freeze' : 'unfreeze'}`)
    },

    async listTransactions(filters: TransactionFilters, suspiciousOnly: boolean) {
        const path = suspiciousOnly ? '/api/admin/transactions/suspicious' : '/api/admin/transactions'

        const response = await transactionClient.get<ApiResponse<PagedResult<AdminTransactionItem>>>(path, {
            params: clean(filters),
        })
        return response.data.data
    },

    async stats() {
        const response = await transactionClient.get<ApiResponse<TransactionStats>>('/api/admin/transactions/stats')
        return response.data.data
    },
}