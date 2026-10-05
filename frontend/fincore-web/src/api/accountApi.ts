import { accountClient } from './client'
import type { Account, ApiResponse, CreateAccountRequest } from './types'

export const accountApi = {
    async list() {
        const response = await accountClient.get<ApiResponse<Account[]>>('/api/accounts')
        return response.data.data
    },

    async get(id: string) {
        const response = await accountClient.get<ApiResponse<Account>>(`/api/accounts/${id}`)
        return response.data.data
    },

    async create(request: CreateAccountRequest) {
        const response = await accountClient.post<ApiResponse<Account>>('/api/accounts', request)
        return response.data.data
    },

    async freeze(id: string) {
        const response = await accountClient.post<ApiResponse<Account>>(`/api/accounts/${id}/freeze`)
        return response.data.data
    },

    async unfreeze(id: string) {
        const response = await accountClient.post<ApiResponse<Account>>(`/api/accounts/${id}/unfreeze`)
        return response.data.data
    },
}