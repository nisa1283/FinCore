import { useState, type FormEvent } from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { adminApi } from '../api/adminApi'
import { AdminTransactionTable } from './AdminTransactionTable'
import { Button } from './Button'
import { TextField } from './TextField'
import { Card, EmptyState, LoadingBlock, PageHeader, Pagination, QueryError, SelectField } from './ui'
import { CATEGORIES } from '../lib/format'

interface FilterForm {
    search: string
    status: string
    category: string
    from: string
    to: string
    minAmount: string
    maxAmount: string
}

const emptyFilters: FilterForm = { search: '', status: '', category: '', from: '', to: '', minAmount: '', maxAmount: '' }

export function AdminTransactionsView({
    suspiciousOnly,
    title,
    subtitle,
}: {
    suspiciousOnly: boolean
    title: string
    subtitle: string
}) {
    const [draft, setDraft] = useState<FilterForm>(emptyFilters)
    const [applied, setApplied] = useState<FilterForm>(emptyFilters)
    const [page, setPage] = useState(1)

    const query = useQuery({
        queryKey: ['admin', 'transactions', suspiciousOnly, applied, page],
        queryFn: () => adminApi.listTransactions({ ...applied, page, pageSize: 15 }, suspiciousOnly),
        placeholderData: keepPreviousData,
    })

    const update = (key: keyof FilterForm) => (value: string) => setDraft((current) => ({ ...current, [key]: value }))

    const handleApply = (event: FormEvent) => {
        event.preventDefault()
        setApplied(draft)
        setPage(1)
    }

    const handleReset = () => {
        setDraft(emptyFilters)
        setApplied(emptyFilters)
        setPage(1)
    }

    const data = query.data

    return (
        <div>
            <PageHeader title={title} subtitle={subtitle} />

            <Card className="mb-6">
                <form onSubmit={handleApply}>
                    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                        <TextField id="search" label="Search description" value={draft.search} onChange={update('search')} />

                        <SelectField
                            id="status"
                            label="Status"
                            value={draft.status}
                            onChange={update('status')}
                            options={[
                                { value: '', label: 'All' },
                                { value: 'Completed', label: 'Completed' },
                                { value: 'Pending', label: 'Pending' },
                                { value: 'Failed', label: 'Failed' },
                            ]}
                        />

                        <SelectField
                            id="category"
                            label="Category"
                            value={draft.category}
                            onChange={update('category')}
                            options={[{ value: '', label: 'All' }, ...CATEGORIES.map((item) => ({ value: item, label: item }))]}
                        />

                        <TextField id="minAmount" label="Min amount" type="number" value={draft.minAmount} onChange={update('minAmount')} />
                        <TextField id="from" label="From date" type="date" value={draft.from} onChange={update('from')} />
                        <TextField id="to" label="To date" type="date" value={draft.to} onChange={update('to')} />
                        <TextField id="maxAmount" label="Max amount" type="number" value={draft.maxAmount} onChange={update('maxAmount')} />
                    </div>

                    <div className="mt-5 flex gap-3">
                        <div className="w-32">
                            <Button type="submit">Apply</Button>
                        </div>
                        <button
                            type="button"
                            onClick={handleReset}
                            className="rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-medium hover:bg-slate-50"
                        >
                            Reset
                        </button>
                    </div>
                </form>
            </Card>

            <Card>
                {query.isLoading && <LoadingBlock className="h-48" />}
                {query.isError && <QueryError error={query.error} onRetry={() => query.refetch()} />}

                {data && data.items.length === 0 && (
                    <EmptyState
                        title={suspiciousOnly ? 'No suspicious transactions' : 'No transactions found'}
                        message={suspiciousOnly ? 'Transactions with a risk score of 60 or more appear here.' : 'Try changing the filters.'}
                    />
                )}

                {data && data.items.length > 0 && (
                    <>
                        <p className="mb-2 text-sm text-slate-500">{data.totalCount} transaction(s)</p>
                        <AdminTransactionTable items={data.items} />
                        <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
                    </>
                )}
            </Card>
        </div>
    )
}