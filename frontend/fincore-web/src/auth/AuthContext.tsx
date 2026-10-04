import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi } from '../api/authApi'
import type { UserProfile } from '../api/types'
import { tokenStorage } from './tokenStorage'

interface AuthContextValue {
    user: UserProfile | null
    isLoading: boolean
    isAdmin: boolean
    login: (email: string, password: string) => Promise<void>
    register: (fullName: string, email: string, password: string) => Promise<void>
    logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<UserProfile | null>(null)
    const [isLoading, setIsLoading] = useState(true)

    // Sayfa açılırken: kayıtlı token varsa kullanıcıyı sunucudan geri yükle
    useEffect(() => {
        const restoreSession = async () => {
            if (!tokenStorage.getAccessToken() && !tokenStorage.getRefreshToken()) {
                setIsLoading(false)
                return
            }

            try {
                setUser(await authApi.getMe())
            } catch {
                tokenStorage.clear()
            } finally {
                setIsLoading(false)
            }
        }

        void restoreSession()
    }, [])

    const login = useCallback(async (email: string, password: string) => {
        const tokens = await authApi.login(email, password)
        tokenStorage.setTokens(tokens.accessToken, tokens.refreshToken)
        setUser(await authApi.getMe())
    }, [])

    const register = useCallback(
        async (fullName: string, email: string, password: string) => {
            await authApi.register(fullName, email, password)
            await login(email, password) // kayıttan sonra otomatik giriş
        },
        [login],
    )

    const logout = useCallback(async () => {
        const refreshToken = tokenStorage.getRefreshToken()

        try {
            if (refreshToken) await authApi.logout(refreshToken)
        } catch {
            // Sunucuya ulaşılamasa bile yerel oturumu kapat
        }

        tokenStorage.clear()
        setUser(null)
    }, [])

    const value = useMemo(
        () => ({ user, isLoading, isAdmin: user?.role === 'Admin', login, register, logout }),
        [user, isLoading, login, register, logout],
    )

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
    const context = useContext(AuthContext)
    if (!context) throw new Error('useAuth must be used inside AuthProvider')
    return context
}