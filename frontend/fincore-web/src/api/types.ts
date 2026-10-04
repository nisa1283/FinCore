// Backend'in her cevapta kullandığı standart zarf
export interface ApiResponse<T> {
    success: boolean
    message?: string | null
    data: T
    errors?: string[] | null
}

export interface PagedResult<T> {
    items: T[]
    page: number
    pageSize: number
    totalCount: number
    totalPages: number
}

export interface AuthResponse {
    accessToken: string
    refreshToken: string
    accessTokenExpiresAt: string
}

export interface UserProfile {
    id: string
    email: string
    fullName: string
    role: 'Customer' | 'Admin'
}