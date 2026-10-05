import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, Copy, Plus, Snowflake, Sun } from 'lucide-react'
import { accountApi } from '../api/accountApi'
import { getErrorMessage } from '../api/errors'
import type { Account } from '../api/types'
import { ErrorAlert } from '../components/Alert'
import { Button } from '../components/Button'
import { TextField } from '../components/TextField'
import { Card, EmptyState, LoadingBlock, Modal, PageHeader, QueryError, SelectField, StatusBadge } from '../components/ui'
import { CURRENCIES, formatMoney } from '../lib/format'

function AccountCard({
    account,
    onToggle,
    toggling,
}: {
    account: Account
    onToggle: (account: Account) => void
    toggling: boolean
}) {
    const [copied, setCopied] = useState(false)
    const frozen = account.status === 'Frozen'

    const copyNumber = async () => {
        try {
            await navigator.clipboard.writeText(account.accountNumber)
            setCopied(true)
            window.setTimeout(() => setCopied(false), 1500)
        } catch {
            // Clipboard access can be blocked by the browser; ignore
        }
    }

    return (
        <Card>
            <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                    <p className="truncate font-semibold">{account.name}</p>
                    <span className="mt-1 inline-block rounded-md bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
                        {account.currency}
                    </span>
                </div>
                <StatusBadge status={account.status} />
            </div>

            <div className="mt-4 flex items-center gap-2 text-xs text-slate-500">
                <span className="truncate font-mono">{account.accountNumber}</span>
                <button
                    onClick={copyNumber}
                    className="shrink-0 rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
                    aria-label="Copy account number"
                >
                    {copied ? <Check className="h-3.5 w-3.5 text-emerald-600" /> : <Copy className="h-3.5 w-3.5" />}
                </button>
            </div>

            <p className="mt-4 text-3xl font-bold tracking-tight">{formatMoney(account.balance, account.currency)}</p>

            <div className="mt-5 flex gap-2">
                <Link
                    to={`/accounts/${account.id}`}
                    className="flex-1 rounded-xl border border-slate-300 px-3 py-2 text-center text-sm font-medium hover:bg-slate-50"
                >
                    Details
                </Link>
                <button
                    onClick={() => onToggle(account)}
                    disabled={toggling}
                    className="flex items-center gap-2 rounded-xl border border-slate-300 px-3 py-2 text-sm font-medium hover:bg-slate-50 disabled:opacity-50"
                >
                    {frozen ? <Sun className="h-4 w-4" /> : <Snowflake className="h-4 w-4" />}
                    {frozen ? 'Unfreeze' : 'Freeze'}
                </button>
            </div>
        </Card>
    )
}

export function AccountsPage() {
    const queryClient = useQueryClient()
    const accountsQuery = useQuery({ queryKey: ['accounts'], queryFn: accountApi.list })

    const [modalOpen, setModalOpen] = useState(false)
    const [name, setName] = useState('')
    const [currency, setCurrency] = useState('TRY')
    const [nameError, setNameError] = useState<string | null>(null)

    const createMutation = useMutation({
        mutationFn: () => accountApi.create({ name: name.trim(), currency }),
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ['accounts'] })
            setModalOpen(false)
            setName('')
            setCurrency('TRY')
        },
    })

    const toggleMutation = useMutation({
        mutationFn: (account: Account) =>
            account.status === 'Active' ? accountApi.freeze(account.id) : accountApi.unfreeze(account.id),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['accounts'] }),
    })

    const openModal = () => {
        createMutation.reset()
        setNameError(null)
        setModalOpen(true)
    }

    const handleCreate = (event: FormEvent) => {
        event.preventDefault()

        if (!name.trim()) {
            setNameError('Account name is required.')
            return
        }

        setNameError(null)
        createMutation.mutate()
    }

    const handleToggle = (account: Account) => {
        if (account.status === 'Active' && !window.confirm('Freeze this account? It cannot send or receive money until unfrozen.')) {
            return
        }
        toggleMutation.mutate(account)
    }

    const accounts = accountsQuery.data ?? []

    return (
        <div>
            <PageHeader
                title="Accounts"
                subtitle="Manage your accounts and balances."
                action={
                    <button
                        onClick={openModal}
                        className="flex items-center gap-2 rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-500"
                    >
                        <Plus className="h-4 w-4" /> New account
                    </button>
                }
            />

            {toggleMutation.isError && (
                <div className="mb-4">
                    <ErrorAlert message={getErrorMessage(toggleMutation.error)} />
                </div>
            )}

            {accountsQuery.isLoading && (
                <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                    <LoadingBlock className="h-52" />
                    <LoadingBlock className="h-52" />
                </div>
            )}

            {accountsQuery.isError && <QueryError error={accountsQuery.error} onRetry={() => accountsQuery.refetch()} />}

            {accountsQuery.isSuccess && accounts.length === 0 && (
                <Card>
                    <EmptyState
                        title="You do not have any accounts yet"
                        message="Open your first account to start sending and receiving money. New demo accounts start with 10,000."
                    />
                </Card>
            )}

            {accounts.length > 0 && (
                <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                    {accounts.map((account) => (
                        <AccountCard
                            key={account.id}
                            account={account}
                            onToggle={handleToggle}
                            toggling={toggleMutation.isPending}
                        />
                    ))}
                </div>
            )}

            <Modal open={modalOpen} onClose={() => setModalOpen(false)} title="Open a new account">
                <form onSubmit={handleCreate} className="space-y-4">
                    {createMutation.isError && <ErrorAlert message={getErrorMessage(createMutation.error)} />}

                    <TextField
                        id="accountName"
                        label="Account name"
                        value={name}
                        onChange={setName}
                        placeholder="e.g. Savings"
                        error={nameError}
                    />

                    <SelectField
                        id="accountCurrency"
                        label="Currency"
                        value={currency}
                        onChange={setCurrency}
                        options={CURRENCIES.map((code) => ({ value: code, label: code }))}
                    />

                    <p className="text-xs text-slate-500">Demo project: every new account starts with a balance of 10,000.</p>

                    <Button type="submit" loading={createMutation.isPending}>
                        Open account
                    </Button>
                </form>
            </Modal>
        </div>
    )
}