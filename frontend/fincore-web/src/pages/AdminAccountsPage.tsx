import { useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { adminApi } from '../api/adminApi'
import { getErrorMessage } from '../api/errors'
import type { AdminAccountItem } from '../api/types'
import { ErrorAlert } from '../components/Alert'
import { Button } from '../components/Button'
import { TextField } from '../components/TextField'
import { Card, EmptyState, LoadingBlock, PageHeader, Pagination, QueryError, SelectField, StatusBadge } from '../components/ui'
import { formatMoney } from '../lib/format'

export function AdminAccountsPage() {
    const queryClient = useQueryClient()
    const [searchParams, setSearchParams] = useSearchParams()
    const userId = searchParams.get('userId') ?? ''

    const [search, setSearch] = useState('')
    const [status, setStatus] = useState('')
    const [applied, setApplied] = useState({ search: '', status: '' })
    const [page, setPage] = useState(1)

    const query = useQuery({
        queryKey: ['admin', 'accounts', applied, userId, page],
        queryFn: () => adminApi.listAccounts({ ...applied, userId, page, pageSize: 10 }),
        placeholderData: keepPreviousData,
    })

    const toggleMutation = useMutation({
        mutationFn: (account: AdminAccountItem) => adminApi.setAccountFrozen(account.id, account.status === 'Active'),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin'] }),
    })

    const handleApply = (event: FormEvent) => {
        event.preventDefault()
        setApplied({ search, status })
        setPage(1)
    }

    const data = query.data

    return (
        <div>
            <PageHeader title="Accounts" subtitle="All accounts on the platform." />

            {userId && (
                <div className="mb-4 flex items-center gap-3 rounded-xl bg-indigo-50 px-4 py-3 text-sm text-indigo-700">
                    <span>
                        Showing accounts of user <span className="font-mono">{userId.slice(0, 8)}</span>
                    </span>
                    <button onClick={() => setSearchParams({})} className="font-semibold underline">
                        Clear
                    </button>
                </div>
            )}

            <Card className="mb-6">
                <form onSubmit={handleApply} className="grid items-end gap-4 sm:grid-cols-3">
                    <TextField id="search" label="Search number or name" value={search} onChange={setSearch} />
                    <SelectField
                        id="status"
                        label="Status"
                        value={status}
                        onChange={setStatus}
                        options={[
                            { value: '', label: 'All' },
                            { value: 'Active', label: 'Active' },
                            { value: 'Frozen', label: 'Frozen' },
                        ]}
                    />
                    <Button type="submit">Apply</Button>
                </form>
            </Card>

            {toggleMutation.isError && (
                <div className="mb-4">
                    <ErrorAlert message={getErrorMessage(toggleMutation.error)} />
                </div>
            )}

            <Card>
                {query.isLoading && <LoadingBlock className="h-48" />}
                {query.isError && <QueryError error={query.error} onRetry={() => query.refetch()} />}

                {data && data.items.length === 0 && <EmptyState title="No accounts found" />}

                {data && data.items.length > 0 && (
                    <>
                        <div className="overflow-x-auto">
                            <table className="w-full min-w-[760px] text-left text-sm">
                                <thead>
                                    <tr className="border-b border-slate-100 text-xs uppercase tracking-wider text-slate-400">
                                        <th className="py-3 pr-4 font-medium">Account</th>
                                        <th className="py-3 pr-4 font-medium">Owner</th>
                                        <th className="py-3 pr-4 font-medium">Balance</th>
                                        <th className="py-3 pr-4 font-medium">Status</th>
                                        <th className="py-3 text-right font-medium">Actions</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100">
                                    {data.items.map((account) => (
                                        <tr key={account.id}>
                                            <td className="py-3 pr-4">
                                                <p className="font-medium">{account.name}</p>
                                                <p className="font-mono text-xs text-slate-500">{account.accountNumber}</p>
                                            </td>
                                            <td className="py-3 pr-4 font-mono text-xs text-slate-600" title={account.userId}>
                                                {account.userId.slice(0, 8)}
                                            </td>
                                            <td className="whitespace-nowrap py-3 pr-4 font-semibold">{formatMoney(account.balance, account.currency)}</td>
                                            <td className="py-3 pr-4">
                                                <StatusBadge status={account.status} />
                                            </td>
                                            <td className="py-3 text-right">
                                                <button
                                                    onClick={() => toggleMutation.mutate(account)}
                                                    disabled={toggleMutation.isPending}
                                                    className="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-medium hover:bg-slate-50 disabled:opacity-50"
                                                >
                                                    {account.status === 'Active' ? 'Freeze' : 'Unfreeze'}
                                                </button>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
                    </>
                )}
            </Card>
        </div>
    )
}