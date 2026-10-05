import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
    Area,
    AreaChart,
    CartesianGrid,
    Cell,
    Pie,
    PieChart,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from 'recharts'
import { ArrowDownLeft, ArrowUpRight } from 'lucide-react'
import { accountApi } from '../api/accountApi'
import { transactionApi } from '../api/transactionApi'
import { useAuth } from '../auth/AuthContext'
import { TransactionList } from '../components/TransactionList'
import { Card, EmptyState, LoadingBlock, QueryError } from '../components/ui'
import { formatCompact, formatMonthLabel, formatMoney } from '../lib/format'

const CHART_COLORS = ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#06b6d4', '#8b5cf6', '#ec4899', '#84cc16', '#64748b']

export function DashboardPage() {
    const { user } = useAuth()
    const [selectedCurrency, setSelectedCurrency] = useState<string | null>(null)

    const accountsQuery = useQuery({ queryKey: ['accounts'], queryFn: accountApi.list })
    const accounts = useMemo(() => accountsQuery.data ?? [], [accountsQuery.data])

    const currencies = useMemo(() => [...new Set(accounts.map((account) => account.currency))], [accounts])

    const currency =
        selectedCurrency && currencies.includes(selectedCurrency)
            ? selectedCurrency
            : currencies.includes('TRY')
                ? 'TRY'
                : currencies[0]

    const cur = currency ?? 'TRY'

    const summaryQuery = useQuery({
        queryKey: ['summary', cur],
        queryFn: () => transactionApi.summary(cur, 6),
        enabled: !!currency,
    })

    const recentQuery = useQuery({
        queryKey: ['transactions', 'recent'],
        queryFn: () => transactionApi.list({ page: 1, pageSize: 5 }),
    })

    const totalBalance = useMemo(
        () => accounts.filter((account) => account.currency === cur).reduce((sum, account) => sum + account.balance, 0),
        [accounts, cur],
    )

    const summary = summaryQuery.data
    const thisMonth = summary ? summary.months[summary.months.length - 1] : undefined

    // End-of-month balances, calculated backwards from today's balance
    const balanceSeries = useMemo(() => {
        const months = summary?.months ?? []
        const series = months.map((month) => ({ month: formatMonthLabel(month.month), balance: 0 }))

        let running = totalBalance
        for (let i = months.length - 1; i >= 0; i--) {
            series[i].balance = Math.round(running * 100) / 100
            running -= months[i].income - months[i].expense
        }

        return series
    }, [summary, totalBalance])

    const categories = summary?.categories ?? []
    const firstName = user?.fullName.split(' ')[0]

    if (accountsQuery.isLoading) {
        return (
            <div className="space-y-4">
                <LoadingBlock className="h-32" />
                <LoadingBlock className="h-72" />
            </div>
        )
    }

    if (accountsQuery.isError) {
        return <QueryError error={accountsQuery.error} onRetry={() => accountsQuery.refetch()} />
    }

    if (accounts.length === 0) {
        return (
            <div>
                <h1 className="text-2xl font-bold tracking-tight">Hello, {firstName}</h1>
                <Card className="mt-6">
                    <EmptyState
                        title="Open your first account"
                        message="Create an account to see your balance, spending insights and recent activity here."
                        action={
                            <Link to="/accounts" className="rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-500">
                                Go to accounts
                            </Link>
                        }
                    />
                </Card>
            </div>
        )
    }

    return (
        <div>
            <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Hello, {firstName}</h1>
                    <p className="mt-1 text-sm text-slate-500">Here is an overview of your finances.</p>
                </div>

                {currencies.length > 1 && (
                    <div className="inline-flex rounded-xl bg-slate-200/70 p-1">
                        {currencies.map((code) => (
                            <button
                                key={code}
                                onClick={() => setSelectedCurrency(code)}
                                className={`rounded-lg px-4 py-1.5 text-sm font-medium transition ${code === cur ? 'bg-white shadow-xs' : 'text-slate-600 hover:text-slate-900'
                                    }`}
                            >
                                {code}
                            </button>
                        ))}
                    </div>
                )}
            </div>

            <div className="grid gap-4 lg:grid-cols-3">
                <div className="rounded-2xl bg-linear-to-br from-indigo-600 via-indigo-600 to-violet-700 p-6 text-white shadow-lg">
                    <p className="text-sm text-indigo-100">Total balance</p>
                    <p className="mt-2 text-4xl font-bold tracking-tight">{formatMoney(totalBalance, cur)}</p>
                    <p className="mt-4 text-xs text-indigo-200">
                        Across {accounts.filter((account) => account.currency === cur).length} account(s)
                    </p>
                </div>

                <Card>
                    <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
                            <ArrowDownLeft className="h-5 w-5" />
                        </div>
                        <p className="text-sm text-slate-500">Income this month</p>
                    </div>
                    <p className="mt-3 text-3xl font-bold tracking-tight text-emerald-600">
                        {summaryQuery.isLoading ? '...' : formatMoney(thisMonth?.income ?? 0, cur)}
                    </p>
                </Card>

                <Card>
                    <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-full bg-rose-50 text-rose-600">
                            <ArrowUpRight className="h-5 w-5" />
                        </div>
                        <p className="text-sm text-slate-500">Expenses this month</p>
                    </div>
                    <p className="mt-3 text-3xl font-bold tracking-tight">
                        {summaryQuery.isLoading ? '...' : formatMoney(thisMonth?.expense ?? 0, cur)}
                    </p>
                </Card>
            </div>

            <div className="mt-4 grid gap-4 lg:grid-cols-3">
                <Card className="lg:col-span-2">
                    <h2 className="font-semibold">Balance over time</h2>
                    <p className="text-xs text-slate-500">End-of-month balance, last 6 months</p>

                    {summaryQuery.isError && <QueryError error={summaryQuery.error} onRetry={() => summaryQuery.refetch()} />}

                    {summaryQuery.isLoading && <LoadingBlock className="mt-4 h-64" />}

                    {summary && (
                        <div className="mt-4 h-64">
                            <ResponsiveContainer width="100%" height="100%">
                                <AreaChart data={balanceSeries} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
                                    <defs>
                                        <linearGradient id="balanceFill" x1="0" y1="0" x2="0" y2="1">
                                            <stop offset="5%" stopColor="#6366f1" stopOpacity={0.35} />
                                            <stop offset="95%" stopColor="#6366f1" stopOpacity={0} />
                                        </linearGradient>
                                    </defs>
                                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
                                    <XAxis dataKey="month" axisLine={false} tickLine={false} tick={{ fontSize: 12, fill: '#64748b' }} />
                                    <YAxis
                                        axisLine={false}
                                        tickLine={false}
                                        width={60}
                                        tick={{ fontSize: 12, fill: '#64748b' }}
                                        tickFormatter={(value) => formatCompact(Number(value))}
                                    />
                                    <Tooltip formatter={(value) => formatMoney(Number(value), cur)} />
                                    <Area type="monotone" dataKey="balance" stroke="#6366f1" strokeWidth={2.5} fill="url(#balanceFill)" />
                                </AreaChart>
                            </ResponsiveContainer>
                        </div>
                    )}
                </Card>

                <Card>
                    <h2 className="font-semibold">Spending by category</h2>
                    <p className="text-xs text-slate-500">Last 6 months</p>

                    {summaryQuery.isLoading && <LoadingBlock className="mt-4 h-64" />}

                    {summary && categories.length === 0 && (
                        <EmptyState title="No spending yet" message="Send a transfer to see your categories here." />
                    )}

                    {categories.length > 0 && (
                        <>
                            <div className="mt-2 h-44">
                                <ResponsiveContainer width="100%" height="100%">
                                    <PieChart>
                                        <Pie data={categories} dataKey="total" nameKey="category" innerRadius={50} outerRadius={78} paddingAngle={3}>
                                            {categories.map((item, index) => (
                                                <Cell key={item.category} fill={CHART_COLORS[index % CHART_COLORS.length]} />
                                            ))}
                                        </Pie>
                                        <Tooltip formatter={(value) => formatMoney(Number(value), cur)} />
                                    </PieChart>
                                </ResponsiveContainer>
                            </div>

                            <ul className="mt-3 space-y-1.5">
                                {categories.slice(0, 5).map((item, index) => (
                                    <li key={item.category} className="flex items-center justify-between text-sm">
                                        <span className="flex items-center gap-2">
                                            <span
                                                className="h-2.5 w-2.5 rounded-full"
                                                style={{ backgroundColor: CHART_COLORS[index % CHART_COLORS.length] }}
                                            />
                                            {item.category}
                                        </span>
                                        <span className="font-medium">{formatMoney(item.total, cur)}</span>
                                    </li>
                                ))}
                            </ul>
                        </>
                    )}
                </Card>
            </div>

            <Card className="mt-4">
                <div className="mb-2 flex items-center justify-between">
                    <h2 className="font-semibold">Recent transactions</h2>
                    <Link to="/transactions" className="text-sm font-semibold text-indigo-600 hover:text-indigo-500">
                        View all
                    </Link>
                </div>

                {recentQuery.isLoading && <LoadingBlock className="h-40" />}
                {recentQuery.isError && <QueryError error={recentQuery.error} onRetry={() => recentQuery.refetch()} />}

                {recentQuery.data && recentQuery.data.items.length === 0 && (
                    <EmptyState title="No transactions yet" message="Your latest activity will show up here." />
                )}

                {recentQuery.data && recentQuery.data.items.length > 0 && <TransactionList items={recentQuery.data.items} />}
            </Card>
        </div>
    )
}