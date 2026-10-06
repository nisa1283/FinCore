import type { ComponentType } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Ban, Flag, ListChecks, TrendingUp, Users, Wallet } from 'lucide-react'
import { adminApi } from '../api/adminApi'
import { AdminTransactionTable } from '../components/AdminTransactionTable'
import { Card, EmptyState, LoadingBlock, PageHeader, QueryError } from '../components/ui'
import { formatMoney } from '../lib/format'

function StatCard({
    label,
    value,
    icon: Icon,
    tone,
}: {
    label: string
    value: string
    icon: ComponentType<{ className?: string }>
    tone: string
}) {
    return (
        <Card>
            <div className="flex items-center gap-3">
                <div className={`flex h-10 w-10 items-center justify-center rounded-full ${tone}`}>
                    <Icon className="h-5 w-5" />
                </div>
                <p className="text-sm text-slate-500">{label}</p>
            </div>
            <p className="mt-3 text-3xl font-bold tracking-tight">{value}</p>
        </Card>
    )
}

export function AdminOverviewPage() {
    const statsQuery = useQuery({ queryKey: ['admin', 'stats'], queryFn: adminApi.stats })

    // We only need the total count, so ask for a single row
    const usersQuery = useQuery({
        queryKey: ['admin', 'users-count'],
        queryFn: () => adminApi.listUsers({ page: 1, pageSize: 1 }),
    })

    const accountsQuery = useQuery({
        queryKey: ['admin', 'accounts-count'],
        queryFn: () => adminApi.listAccounts({ page: 1, pageSize: 1 }),
    })

    const suspiciousQuery = useQuery({
        queryKey: ['admin', 'suspicious-preview'],
        queryFn: () => adminApi.listTransactions({ page: 1, pageSize: 5 }, true),
    })

    const stats = statsQuery.data
    const show = (value: number | undefined) => (value === undefined ? '...' : value.toLocaleString('en-US'))

    return (
        <div>
            <PageHeader title="Admin Overview" subtitle="A snapshot of the whole platform." />

            {statsQuery.isError && <QueryError error={statsQuery.error} onRetry={() => statsQuery.refetch()} />}

            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                <StatCard label="Total users" value={show(usersQuery.data?.totalCount)} icon={Users} tone="bg-indigo-50 text-indigo-600" />
                <StatCard label="Total accounts" value={show(accountsQuery.data?.totalCount)} icon={Wallet} tone="bg-sky-50 text-sky-600" />
                <StatCard label="Transactions" value={show(stats?.totalCount)} icon={ListChecks} tone="bg-violet-50 text-violet-600" />
                <StatCard label="Failed transactions" value={show(stats?.failedCount)} icon={Ban} tone="bg-amber-50 text-amber-600" />
                <StatCard label="Suspicious transactions" value={show(stats?.suspiciousCount)} icon={Flag} tone="bg-rose-50 text-rose-600" />

                <Card>
                    <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
                            <TrendingUp className="h-5 w-5" />
                        </div>
                        <p className="text-sm text-slate-500">Completed volume</p>
                    </div>

                    {!stats && <p className="mt-3 text-3xl font-bold tracking-tight">...</p>}
                    {stats && stats.volumeByCurrency.length === 0 && <p className="mt-3 text-sm text-slate-500">No completed transfers yet.</p>}
                    {stats && stats.volumeByCurrency.length > 0 && (
                        <ul className="mt-3 space-y-1">
                            {stats.volumeByCurrency.map((item) => (
                                <li key={item.currency} className="text-xl font-bold tracking-tight">
                                    {formatMoney(item.total, item.currency)}
                                </li>
                            ))}
                        </ul>
                    )}
                </Card>
            </div>

            <Card className="mt-6">
                <div className="mb-2 flex items-center justify-between">
                    <h2 className="font-semibold">Latest suspicious transactions</h2>
                    <Link to="/admin/suspicious" className="text-sm font-semibold text-indigo-600 hover:text-indigo-500">
                        View all
                    </Link>
                </div>

                {suspiciousQuery.isLoading && <LoadingBlock className="h-40" />}
                {suspiciousQuery.isError && <QueryError error={suspiciousQuery.error} />}

                {suspiciousQuery.data && suspiciousQuery.data.items.length === 0 && (
                    <EmptyState title="Nothing suspicious" message="No transaction has reached a risk score of 60 yet." />
                )}

                {suspiciousQuery.data && suspiciousQuery.data.items.length > 0 && (
                    <AdminTransactionTable items={suspiciousQuery.data.items} />
                )}
            </Card>
        </div>
    )
}