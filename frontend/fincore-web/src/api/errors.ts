import axios from 'axios'
import type { ApiResponse } from './types'

export function getErrorMessage(error: unknown): string {
    if (axios.isAxiosError(error)) {
        if (!error.response) {
            return 'Cannot reach the server. Please check that the backend is running.'
        }

        const data = error.response.data as ApiResponse<unknown> | undefined

        if (data?.errors && data.errors.length > 0) return data.errors.join(' ')
        if (data?.message) return data.message
    }

    return 'Something went wrong. Please try again.'
}