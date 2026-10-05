import { useState } from 'react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowDownLeft, ArrowUpRight, CheckCheck } from 'lucide-react'
import { notificationApi } from '../api/notificationApi'
import { Card, EmptyState, LoadingBlock, PageHeader, Pagination, QueryError } from '../components/ui'
import { formatDateTime } from '../lib/format'

export function NotificationsPage() {
    const queryClient = useQueryClient()
    const [unreadOnly, setUnreadOnly] = useState(false)
    const [page, setPage] = useState(1)

    const query = useQuery({
        queryKey: ['notifications', unreadOnly, page],
        queryFn: () => notificationApi.list({ page, pageSize: 10, unreadOnly }),
        placeholderData: keepPreviousData,
    })

    const refresh = async () => {
        await queryClient.invalidateQueries({ queryKey: ['notifications'] })
        await queryClient.invalidateQueries({ queryKey: ['unread-count'] })
    }

    const readMutation = useMutation({ mutationFn: (id: string) => notificationApi.markRead(id), onSuccess: refresh })
    const readAllMutation = useMutation({ mutationFn: () => notificationApi.markAllRead(), onSuccess: refresh })

    const data = query.data

    const switchTab = (value: boolean) => {
        setUnreadOnly(value)
        setPage(1)
    }

    return (
        <div>
            <PageHeader
                title="Notifications"
                subtitle="Updates about your transfers."
                action={
                    <button
                        onClick={() => readAllMutation.mutate()}
                        disabled={readAllMutation.isPending}
                        className="flex items-center gap-2 rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-medium hover:bg-slate-50 disabled:opacity-50"
                    >
                        <CheckCheck className="h-4 w-4" /> Mark all as read
                    </button>
                }
            />

            <div className="mb-4 inline-flex rounded-xl bg-slate-200/70 p-1">
                {[
                    { label: 'All', value: false },
                    { label: 'Unread', value: true },
                ].map((tab) => (
                    <button
                        key={tab.label}
                        onClick={() => switchTab(tab.value)}
                        className={`rounded-lg px-4 py-1.5 text-sm font-medium transition ${unreadOnly === tab.value ? 'bg-white shadow-xs' : 'text-slate-600 hover:text-slate-900'
                            }`}
                    >
                        {tab.label}
                    </button>
                ))}
            </div>

            <Card>
                {query.isLoading && <LoadingBlock className="h-48" />}
                {query.isError && <QueryError error={query.error} onRetry={() => query.refetch()} />}

                {data && data.items.length === 0 && (
                    <EmptyState
                        title={unreadOnly ? 'No unread notifications' : 'No notifications yet'}
                        message="You will be notified here when money is sent or received."
                    />
                )}

                {data && data.items.length > 0 && (
                    <>
                        <ul className="divide-y divide-slate-100">
                            {data.items.map((notification) => {
                                const received = notification.type === 'TransferReceived'

                                return (
                                    <li key={notification.id}>
                                        <button
                                            onClick={() => !notification.isRead && readMutation.mutate(notification.id)}
                                            className={`flex w-full items-start gap-4 rounded-xl px-2 py-3.5 text-left transition ${notification.isRead ? '' : 'bg-indigo-50/50 hover:bg-indigo-50'
                                                }`}
                                        >
                                            <div
                                                className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full ${received ? 'bg-emerald-50 text-emerald-600' : 'bg-slate-100 text-slate-600'
                                                    }`}
                                            >
                                                {received ? <ArrowDownLeft className="h-5 w-5" /> : <ArrowUpRight className="h-5 w-5" />}
                                            </div>

                                            <div className="min-w-0 flex-1">
                                                <p className="text-sm font-semibold">{notification.title}</p>
                                                <p className="text-sm text-slate-600">{notification.message}</p>
                                                <p className="mt-1 text-xs text-slate-400">{formatDateTime(notification.createdAt)}</p>
                                            </div>

                                            {!notification.isRead && <span className="mt-2 h-2.5 w-2.5 shrink-0 rounded-full bg-indigo-600" />}
                                        </button>
                                    </li>
                                )
                            })}
                        </ul>

                        <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
                    </>
                )}
            </Card>
        </div>
    )
}