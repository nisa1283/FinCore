import { useCallback, useEffect, useState } from 'react'
import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { notificationApi } from '../api/notificationApi'
import type { AppNotification } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { tokenStorage } from '../auth/tokenStorage'

const HUB_URL = `${import.meta.env.VITE_NOTIFICATION_API_URL ?? 'http://localhost:5004'}/hubs/notifications`

export interface Toast {
    id: string
    title: string
    message: string
}

export function useNotificationHub() {
    const { user } = useAuth()
    const queryClient = useQueryClient()
    const [toasts, setToasts] = useState<Toast[]>([])
    const userId = user?.id

    const dismissToast = useCallback((id: string) => {
        setToasts((current) => current.filter((toast) => toast.id !== id))
    }, [])

    useEffect(() => {
        if (!userId) return

        let cancelled = false
        let connection: HubConnection | null = null
        let retryTimer: number | undefined

        const scheduleRetry = () => {
            if (!cancelled) retryTimer = window.setTimeout(() => void connect(), 5000)
        }

        const connect = async () => {
            try {
                // A normal API call first: if the access token expired, the axios interceptor refreshes it
                await notificationApi.getUnreadCount()
                if (cancelled) return

                const hub = new HubConnectionBuilder()
                    .withUrl(HUB_URL, { accessTokenFactory: () => tokenStorage.getAccessToken() ?? '' })
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.Warning)
                    .build()

                hub.on('notificationReceived', (notification: AppNotification) => {
                    const toast: Toast = { id: notification.id, title: notification.title, message: notification.message }

                    setToasts((current) => [...current.filter((item) => item.id !== toast.id), toast].slice(-3))
                    window.setTimeout(() => dismissToast(toast.id), 6000)

                    // Money moved: refresh everything that depends on it
                    for (const key of ['unread-count', 'notifications', 'accounts', 'transactions', 'summary']) {
                        void queryClient.invalidateQueries({ queryKey: [key] })
                    }
                })

                hub.onclose(scheduleRetry)
                connection = hub
                await hub.start()
            } catch {
                scheduleRetry()
            }
        }

        void connect()

        return () => {
            cancelled = true
            window.clearTimeout(retryTimer)
            void connection?.stop()
        }
    }, [userId, queryClient, dismissToast])

    return { toasts, dismissToast }
}