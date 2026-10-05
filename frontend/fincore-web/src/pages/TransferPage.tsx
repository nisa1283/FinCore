import { useState } from 'react'
import axios from 'axios'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check } from 'lucide-react'
import { accountApi } from '../api/accountApi'
import { getErrorMessage } from '../api/errors'
import type { TransferResult } from '../api/types'
import { transactionApi } from '../api/transactionApi'
import { ErrorAlert } from '../components/Alert'
import { Button } from '../components/Button'
import { TextField } from '../components/TextField'
import { Card, EmptyState, LoadingBlock, Modal, PageHeader, QueryError, SelectField } from '../components/ui'
import { CATEGORIES, formatMoney } from '../lib/format'

export function TransferPage() {
    const queryClient = useQueryClient()
    const accountsQuery = useQuery({ queryKey: ['accounts'], queryFn: accountApi.list })

    const [sourceId, setSourceId] = useState('')
    const [target, setTarget] = useState('')
    const [amount, setAmount] = useState('')
    const [description, setDescription] = useState('')
    const [category, setCategory] = useState('General')
    const [errors, setErrors] = useState<Record<string, string>>({})
    const [reviewOpen, setReviewOpen] = useState(false)
    const [result, setResult] = useState<TransferResult | null>(null)
    const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID())

    const activeAccounts = (accountsQuery.data ?? []).filter((account) => account.status === 'Active')
    const effectiveSourceId = sourceId || activeAccounts[0]?.id || ''
    const source = activeAccounts.find((account) => account.id === effectiveSourceId)
    const normalizedTarget = target.replace(/\s+/g, '').toUpperCase()
    const amountNumber = Number(amount.replace(',', '.'))

    const mutation = useMutation({
        mutationFn: () =>
            transactionApi.transfer(idempotencyKey, {
                sourceAccountId: effectiveSourceId,
                targetAccountNumber: normalizedTarget,
                amount: amountNumber,
                description: description.trim() || undefined,
                category,
            }),
        onSuccess: async (data) => {
            setResult(data)
            setReviewOpen(false)
            setIdempotencyKey(crypto.randomUUID())

            for (const key of ['accounts', 'transactions', 'summary']) {
                await queryClient.invalidateQueries({ queryKey: [key] })
            }
        },
        onError: (error) => {
            setReviewOpen(false)

            // The server answered, so this attempt is finished: use a fresh key next time.
            // No response (network error) means we do not know the outcome: keep the key so a retry is safe.
            if (axios.isAxiosError(error) && error.response) {
                setIdempotencyKey(crypto.randomUUID())
            }
        },
    })

    const validate = () => {
        const found: Record<string, string> = {}

        if (!source) found.source = 'Select a source account.'

        if (!/^TR\d{24}$/.test(normalizedTarget)) {
            found.target = 'Enter a valid account number (TR followed by 24 digits).'
        } else if (source && normalizedTarget === source.accountNumber) {
            found.target = 'Source and target accounts must be different.'
        }

        if (!/^\d+([.,]\d{1,2})?$/.test(amount.trim()) || amountNumber <= 0) {
            found.amount = 'Enter an amount greater than zero (up to 2 decimals).'
        } else if (source && amountNumber > source.balance) {
            found.amount = 'Insufficient balance.'
        }

        if (description.length > 200) found.description = 'Description can be at most 200 characters.'

        return found
    }

    const handleReview = () => {
        const found = validate()
        setErrors(found)

        if (Object.keys(found).length > 0) return

        mutation.reset()
        setReviewOpen(true)
    }

    const resetForm = () => {
        setResult(null)
        setTarget('')
        setAmount('')
        setDescription('')
        setCategory('General')
        setErrors({})
        mutation.reset()
    }

    if (accountsQuery.isLoading) return <LoadingBlock className="h-64" />
    if (accountsQuery.isError) return <QueryError error={accountsQuery.error} onRetry={() => accountsQuery.refetch()} />

    if (result) {
        const completed = result.status === 'Completed'

        return (
            <div className="mx-auto max-w-lg">
                <Card className="text-center">
                    <div
                        className={`mx-auto flex h-14 w-14 items-center justify-center rounded-full ${completed ? 'bg-emerald-50 text-emerald-600' : 'bg-rose-50 text-rose-600'
                            }`}
                    >
                        <Check className="h-7 w-7" />
                    </div>

                    <h2 className="mt-4 text-xl font-bold">{completed ? 'Transfer completed' : 'Transfer failed'}</h2>
                    <p className="mt-1 text-3xl font-bold tracking-tight">{formatMoney(result.amount, result.currency ?? 'TRY')}</p>
                    <p className="mt-2 text-sm text-slate-500">
                        To <span className="font-mono">{result.targetAccountNumber}</span>
                    </p>
                    {result.failureReason && <p className="mt-2 text-sm text-rose-600">{result.failureReason}</p>}
                    {result.isDuplicate && (
                        <p className="mt-2 text-xs text-slate-500">This request was already processed. Showing the original result.</p>
                    )}

                    <div className="mt-6 flex gap-3">
                        <button
                            onClick={resetForm}
                            className="flex-1 rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-500"
                        >
                            New transfer
                        </button>
                        <Link
                            to="/transactions"
                            className="flex-1 rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-medium hover:bg-slate-50"
                        >
                            View transactions
                        </Link>
                    </div>
                </Card>
            </div>
        )
    }

    if (activeAccounts.length === 0) {
        return (
            <div>
                <PageHeader title="Transfer" subtitle="Send money to another account." />
                <Card>
                    <EmptyState
                        title="No active account available"
                        message="You need at least one active account to send money."
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
        <div className="mx-auto max-w-xl">
            <PageHeader title="Transfer" subtitle="Send money to another account." />

            <Card>
                <div className="space-y-5">
                    {mutation.isError && <ErrorAlert message={getErrorMessage(mutation.error)} />}

                    <SelectField
                        id="source"
                        label="From account"
                        value={effectiveSourceId}
                        onChange={setSourceId}
                        error={errors.source}
                        options={activeAccounts.map((account) => ({
                            value: account.id,
                            label: `${account.name} - ${formatMoney(account.balance, account.currency)}`,
                        }))}
                    />

                    <TextField
                        id="target"
                        label="To account number"
                        value={target}
                        onChange={setTarget}
                        placeholder="TR000000000000000000000000"
                        error={errors.target}
                    />

                    <TextField
                        id="amount"
                        label={`Amount${source ? ` (${source.currency})` : ''}`}
                        value={amount}
                        onChange={setAmount}
                        placeholder="0.00"
                        error={errors.amount}
                    />

                    <SelectField
                        id="category"
                        label="Category"
                        value={category}
                        onChange={setCategory}
                        options={CATEGORIES.map((item) => ({ value: item, label: item }))}
                    />

                    <TextField
                        id="description"
                        label="Description (optional)"
                        value={description}
                        onChange={setDescription}
                        placeholder="What is this for?"
                        error={errors.description}
                    />

                    <Button type="button" onClick={handleReview}>
                        Review transfer
                    </Button>
                </div>
            </Card>

            <Modal open={reviewOpen} onClose={() => setReviewOpen(false)} title="Confirm transfer">
                <dl className="space-y-3 text-sm">
                    <div className="flex justify-between gap-4">
                        <dt className="text-slate-500">From</dt>
                        <dd className="text-right font-medium">{source?.name}</dd>
                    </div>
                    <div className="flex justify-between gap-4">
                        <dt className="text-slate-500">To</dt>
                        <dd className="break-all text-right font-mono text-xs font-medium">{normalizedTarget}</dd>
                    </div>
                    <div className="flex justify-between gap-4">
                        <dt className="text-slate-500">Amount</dt>
                        <dd className="text-lg font-bold">{formatMoney(amountNumber || 0, source?.currency ?? 'TRY')}</dd>
                    </div>
                    <div className="flex justify-between gap-4">
                        <dt className="text-slate-500">Category</dt>
                        <dd className="font-medium">{category}</dd>
                    </div>
                    {description.trim() && (
                        <div className="flex justify-between gap-4">
                            <dt className="text-slate-500">Description</dt>
                            <dd className="text-right font-medium">{description.trim()}</dd>
                        </div>
                    )}
                </dl>

                <div className="mt-6 flex gap-3">
                    <button
                        onClick={() => setReviewOpen(false)}
                        disabled={mutation.isPending}
                        className="flex-1 rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-medium hover:bg-slate-50 disabled:opacity-50"
                    >
                        Back
                    </button>
                    <Button type="button" loading={mutation.isPending} onClick={() => mutation.mutate()} className="flex-1">
                        Confirm and send
                    </Button>
                </div>
            </Modal>
        </div>
    )
}