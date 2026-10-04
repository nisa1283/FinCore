import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { tokenStorage } from '../auth/tokenStorage'
import type { ApiResponse, AuthResponse } from './types'

const AUTH_URL = import.meta.env.VITE_AUTH_API_URL ?? 'http://localhost:5001'
const ACCOUNT_URL = import.meta.env.VITE_ACCOUNT_API_URL ?? 'http://localhost:5002'
const TRANSACTION_URL = import.meta.env.VITE_TRANSACTION_API_URL ?? 'http://localhost:5003'
const NOTIFICATION_URL = import.meta.env.VITE_NOTIFICATION_API_URL ?? 'http://localhost:5004'

// Aynı anda birden çok istek 401 alırsa token'ı bir kez yenile, hepsi sonucu paylaşsın
let refreshPromise: Promise<string> | null = null

async function refreshAccessToken(): Promise<string> {
    const refreshToken = tokenStorage.getRefreshToken()
    if (!refreshToken) throw new Error('No refresh token')

    // Burada interceptor'lı istemci değil, düz axios kullanıyoruz (sonsuz döngüyü önlemek için)
    const response = await axios.post<ApiResponse<AuthResponse>>(`${AUTH_URL}/api/auth/refresh`, { refreshToken })
    const tokens = response.data.data

    tokenStorage.setTokens(tokens.accessToken, tokens.refreshToken)
    return tokens.accessToken
}

type RetriableConfig = InternalAxiosRequestConfig & { _retry?: boolean }

function createClient(baseURL: string) {
    const client = axios.create({ baseURL, timeout: 15000 })

    // 1) Her isteğe token ekle
    client.interceptors.request.use((config) => {
        const token = tokenStorage.getAccessToken()
        if (token) config.headers.Authorization = `Bearer ${token}`
        return config
    })

    // 2) 401 gelirse token'ı yenileyip isteği tekrar dene
    client.interceptors.response.use(
        (response) => response,
        async (error: AxiosError) => {
            const original = error.config as RetriableConfig | undefined

            const isLoginOrRegister = ['/api/auth/login', '/api/auth/register'].some((path) =>
                original?.url?.includes(path),
            )

            if (
                error.response?.status === 401 &&
                original &&
                !original._retry &&
                !isLoginOrRegister &&
                tokenStorage.getRefreshToken()
            ) {
                original._retry = true

                try {
                    refreshPromise ??= refreshAccessToken().finally(() => {
                        refreshPromise = null
                    })

                    const newToken = await refreshPromise
                    original.headers.Authorization = `Bearer ${newToken}`
                    return client(original)
                } catch {
                    // Yenileme de başarısız: oturum bitti
                    tokenStorage.clear()
                    window.location.href = '/login'
                }
            }

            return Promise.reject(error)
        },
    )

    return client
}

export const authClient = createClient(AUTH_URL)
export const accountClient = createClient(ACCOUNT_URL)
export const transactionClient = createClient(TRANSACTION_URL)
export const notificationClient = createClient(NOTIFICATION_URL)