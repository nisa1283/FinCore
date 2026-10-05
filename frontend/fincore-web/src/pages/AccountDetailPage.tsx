import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ArrowLeft, Send } from 'lucide-react'
import { accountApi } from '../api/accountApi'
import { transactionApi } from '../api/transactionApi'
import { TransactionList } from '../components/TransactionList'
import { Card, EmptyState, LoadingBlock, Pagination, QueryError, StatusBadge } from '../components/ui'
import { formatDate, formatMoney } from '../lib/format'

export function AccountDetailPage() {
    const { id = '' } = useParams()
    const [page, setPage] = useState(1)

    const accountQuery = useQuery({ queryKey: ['accounts', id], queryFn: () => accountApi.get(id), enabled: !!id })

    const transactionsQuery = useQuery({
        queryKey: ['transactions', 'account', id, page],
        queryFn: () => transactionApi.list({ page, pageSize: 10, accountId: id }),
        enabled: !!id,
        placeholderData: keepPreviousData,
    })

    const account = accountQuery.data
    const transactions = transactionsQuery.data

    return (
        <div>
            <Link to="/accounts" className="mb-4 inline-flex items-center gap-1 text-sm font-medium text-slate-500 hover:text-slate-900">
                <ArrowLeft className="h-4 w-4" /> Back to accounts
            </Link>

            {accountQuery.isLoading && <LoadingBlock className="h-40" />}
            {accountQuery.isError && <QueryError error={accountQuery.error} onRetry={() => accountQuery.refetch()} />}

            {account && (
                <Card>
                    <div className="flex flex-wrap items-start justify-between gap-4">
                        <div>
                            <div className="flex items-center gap-3">
                                <h1 className="text-2xl font-bold tracking-tight">{account.name}</h1>
                                <StatusBadge status={account.status} />
                            </div>
                            <p className="mt-1 font-mono text-sm text-slate-500">{account.accountNumber}</p>
                            <p className="mt-1 text-xs text-slate-400">Opened {formatDate(account.createdAt)}</p>
                        </div>

                        <div className="text-right">
                            <p className="text-sm text-slate-500">Balance</p>
                            <p className="text-3xl font-bold tracking-tight">{formatMoney(account.balance, account.currency)}</p>
                        </div>
                    </div>

                    <Link
                        to="/transfer"
                        className="mt-5 inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-indigo-500"
                    >
                        <Send className="h-4 w-4" /> Send money
                    </Link>
                </Card>
            )}

            <h2 className="mb-3 mt-8 text-lg font-semibold">Transactions</h2>

            <Card>
                {transactionsQuery.isLoading && <LoadingBlock className="h-40" />}
                {transactionsQuery.isError && <QueryError error={transactionsQuery.error} />}

                {transactions && transactions.items.length === 0 && (
                    <EmptyState title="No transactions yet" message="Transactions for this account will appear here." />
                )}

                {transactions && transactions.items.length > 0 && (
                    <>
                        <TransactionList items={transactions.items} />
                        <Pagination page={transactions.page} totalPages={transactions.totalPages} onChange={setPage} />
                    </>
                )}
            </Card>
        </div>
    )
}