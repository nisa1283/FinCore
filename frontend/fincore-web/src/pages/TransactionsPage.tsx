import { useState, type FormEvent } from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { transactionApi } from '../api/transactionApi'
import { Button } from '../components/Button'
import { TextField } from '../components/TextField'
import { TransactionList } from '../components/TransactionList'
import { Card, EmptyState, LoadingBlock, PageHeader, Pagination, QueryError, SelectField } from '../components/ui'
import { CATEGORIES } from '../lib/format'

interface FilterForm {
    search: string
    status: string
    type: string
    category: string
    from: string
    to: string
    minAmount: string
    maxAmount: string
}

const emptyFilters: FilterForm = {
    search: '',
    status: '',
    type: '',
    category: '',
    from: '',
    to: '',
    minAmount: '',
    maxAmount: '',
}

export function TransactionsPage() {
    const [draft, setDraft] = useState<FilterForm>(emptyFilters)
    const [applied, setApplied] = useState<FilterForm>(emptyFilters)
    const [page, setPage] = useState(1)

    const query = useQuery({
        queryKey: ['transactions', 'list', applied, page],
        queryFn: () => transactionApi.list({ ...applied, page, pageSize: 10 }),
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
            <PageHeader title="Transactions" subtitle="Your complete transaction history." />

            <Card className="mb-6">
                <form onSubmit={handleApply}>
                    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                        <TextField id="search" label="Search description" value={draft.search} onChange={update('search')} placeholder="e.g. dinner" />

                        <SelectField
                            id="type"
                            label="Type"
                            value={draft.type}
                            onChange={update('type')}
                            options={[
                                { value: '', label: 'All' },
                                { value: 'Incoming', label: 'Incoming' },
                                { value: 'Outgoing', label: 'Outgoing' },
                            ]}
                        />

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

                        <TextField id="from" label="From date" type="date" value={draft.from} onChange={update('from')} />
                        <TextField id="to" label="To date" type="date" value={draft.to} onChange={update('to')} />
                        <TextField id="minAmount" label="Min amount" type="number" value={draft.minAmount} onChange={update('minAmount')} placeholder="0" />
                        <TextField id="maxAmount" label="Max amount" type="number" value={draft.maxAmount} onChange={update('maxAmount')} placeholder="10000" />
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
                    <EmptyState title="No transactions found" message="Try changing or resetting the filters." />
                )}

                {data && data.items.length > 0 && (
                    <>
                        <p className="mb-2 text-sm text-slate-500">{data.totalCount} transaction(s)</p>
                        <TransactionList items={data.items} />
                        <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
                    </>
                )}
            </Card>
        </div>
    )
}