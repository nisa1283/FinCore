import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { adminApi } from '../api/adminApi'
import { getErrorMessage } from '../api/errors'
import type { AdminUserItem } from '../api/types'
import { ErrorAlert } from '../components/Alert'
import { Button } from '../components/Button'
import { TextField } from '../components/TextField'
import { Card, EmptyState, LoadingBlock, PageHeader, Pagination, QueryError, SelectField, StatusBadge } from '../components/ui'
import { formatDate } from '../lib/format'

export function AdminCustomersPage() {
    const queryClient = useQueryClient()
    const [search, setSearch] = useState('')
    const [isActive, setIsActive] = useState('')
    const [applied, setApplied] = useState({ search: '', isActive: '' })
    const [page, setPage] = useState(1)

    const query = useQuery({
        queryKey: ['admin', 'users', applied, page],
        queryFn: () => adminApi.listUsers({ ...applied, page, pageSize: 10 }),
        placeholderData: keepPreviousData,
    })

    const refresh = () => queryClient.invalidateQueries({ queryKey: ['admin'] })

    const activeMutation = useMutation({
        mutationFn: (variables: { id: string; active: boolean }) => adminApi.setUserActive(variables.id, variables.active),
        onSuccess: refresh,
    })

    const unlockMutation = useMutation({
        mutationFn: (id: string) => adminApi.unlockUser(id),
        onSuccess: refresh,
    })

    const handleApply = (event: FormEvent) => {
        event.preventDefault()
        setApplied({ search, isActive })
        setPage(1)
    }

    const handleToggle = (user: AdminUserItem) => {
        if (user.isActive && !window.confirm(`Deactivate ${user.email}? They will be signed out and cannot log in.`)) return
        activeMutation.mutate({ id: user.id, active: !user.isActive })
    }

    const data = query.data
    const actionError = activeMutation.error ?? unlockMutation.error

    return (
        <div>
            <PageHeader title="Customers" subtitle="Manage platform users." />

            <Card className="mb-6">
                <form onSubmit={handleApply} className="grid items-end gap-4 sm:grid-cols-3">
                    <TextField id="search" label="Search name or email" value={search} onChange={setSearch} />
                    <SelectField
                        id="isActive"
                        label="Status"
                        value={isActive}
                        onChange={setIsActive}
                        options={[
                            { value: '', label: 'All' },
                            { value: 'true', label: 'Active' },
                            { value: 'false', label: 'Inactive' },
                        ]}
                    />
                    <Button type="submit">Apply</Button>
                </form>
            </Card>

            {actionError && (
                <div className="mb-4">
                    <ErrorAlert message={getErrorMessage(actionError)} />
                </div>
            )}

            <Card>
                {query.isLoading && <LoadingBlock className="h-48" />}
                {query.isError && <QueryError error={query.error} onRetry={() => query.refetch()} />}

                {data && data.items.length === 0 && <EmptyState title="No users found" message="Try a different search." />}

                {data && data.items.length > 0 && (
                    <>
                        <div className="overflow-x-auto">
                            <table className="w-full min-w-[760px] text-left text-sm">
                                <thead>
                                    <tr className="border-b border-slate-100 text-xs uppercase tracking-wider text-slate-400">
                                        <th className="py-3 pr-4 font-medium">User</th>
                                        <th className="py-3 pr-4 font-medium">Role</th>
                                        <th className="py-3 pr-4 font-medium">Status</th>
                                        <th className="py-3 pr-4 font-medium">Joined</th>
                                        <th className="py-3 text-right font-medium">Actions</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-100">
                                    {data.items.map((user) => (
                                        <tr key={user.id}>
                                            <td className="py-3 pr-4">
                                                <p className="font-medium">{user.fullName}</p>
                                                <p className="text-xs text-slate-500">{user.email}</p>
                                            </td>
                                            <td className="py-3 pr-4 text-slate-600">{user.role}</td>
                                            <td className="py-3 pr-4">
                                                <div className="flex gap-1.5">
                                                    <StatusBadge status={user.isActive ? 'Active' : 'Inactive'} />
                                                    {user.isLocked && <StatusBadge status="Locked" />}
                                                </div>
                                            </td>
                                            <td className="whitespace-nowrap py-3 pr-4 text-slate-600">{formatDate(user.createdAt)}</td>
                                            <td className="py-3">
                                                <div className="flex justify-end gap-2">
                                                    <Link
                                                        to={`/admin/accounts?userId=${user.id}`}
                                                        className="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-medium hover:bg-slate-50"
                                                    >
                                                        Accounts
                                                    </Link>
                                                    {user.isLocked && (
                                                        <button
                                                            onClick={() => unlockMutation.mutate(user.id)}
                                                            disabled={unlockMutation.isPending}
                                                            className="rounded-lg border border-amber-300 px-3 py-1.5 text-xs font-medium text-amber-700 hover:bg-amber-50 disabled:opacity-50"
                                                        >
                                                            Unlock
                                                        </button>
                                                    )}
                                                    <button
                                                        onClick={() => handleToggle(user)}
                                                        disabled={activeMutation.isPending}
                                                        className="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-medium hover:bg-slate-50 disabled:opacity-50"
                                                    >
                                                        {user.isActive ? 'Deactivate' : 'Activate'}
                                                    </button>
                                                </div>
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