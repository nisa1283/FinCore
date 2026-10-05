import { transactionClient } from './client'
import type {
    ApiResponse,
    PagedResult,
    TransactionFilters,
    TransactionItem,
    TransactionSummary,
    TransferRequest,
    TransferResult,
} from './types'

export const transactionApi = {
    async transfer(idempotencyKey: string, request: TransferRequest) {
        const response = await transactionClient.post<ApiResponse<TransferResult>>(
            '/api/transactions/transfer',
            request,
            { headers: { 'Idempotency-Key': idempotencyKey } },
        )
        return response.data.data
    },

    async list(filters: TransactionFilters) {
        // Do not send empty filters to the API
        const params = Object.fromEntries(
            Object.entries(filters).filter(([, value]) => value !== '' && value !== undefined),
        )

        const response = await transactionClient.get<ApiResponse<PagedResult<TransactionItem>>>('/api/transactions', {
            params,
        })
        return response.data.data
    },

    async summary(currency: string, months = 6) {
        const response = await transactionClient.get<ApiResponse<TransactionSummary>>('/api/transactions/summary', {
            params: { currency, months },
        })
        return response.data.data
    },
}