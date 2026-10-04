import { authClient } from './client'
import type { ApiResponse, AuthResponse, UserProfile } from './types'

export const authApi = {
    async login(email: string, password: string) {
        const response = await authClient.post<ApiResponse<AuthResponse>>('/api/auth/login', { email, password })
        return response.data.data
    },

    async register(fullName: string, email: string, password: string) {
        const response = await authClient.post<ApiResponse<UserProfile>>('/api/auth/register', {
            email,
            password,
            fullName,
        })
        return response.data.data
    },

    async getMe() {
        const response = await authClient.get<ApiResponse<UserProfile>>('/api/users/me')
        return response.data.data
    },

    async logout(refreshToken: string) {
        await authClient.post('/api/auth/logout', { refreshToken })
    },
}