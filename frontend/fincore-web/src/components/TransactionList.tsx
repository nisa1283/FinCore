import { ArrowDownLeft, ArrowUpRight } from 'lucide-react'
import type { TransactionItem } from '../api/types'
import { formatDateTime, formatMoney } from '../lib/format'
import { StatusBadge } from './ui'

export function TransactionList({ items }: { items: TransactionItem[] }) {
    return (
        <ul className="divide-y divide-slate-100">
            {items.map((transaction) => {
                const incoming = transaction.type === 'Incoming'
                const failed = transaction.status === 'Failed'
                const title = transaction.description || (incoming ? 'Money received' : 'Transfer sent')

                return (
                    <li key={transaction.id} className="flex items-center gap-4 py-3.5">
                        <div
                            className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full ${incoming ? 'bg-emerald-50 text-emerald-600' : 'bg-slate-100 text-slate-600'
                                }`}
                        >
                            {incoming ? <ArrowDownLeft className="h-5 w-5" /> : <ArrowUpRight className="h-5 w-5" />}
                        </div>

                        <div className="min-w-0 flex-1">
                            <p className="truncate text-sm font-medium">{title}</p>
                            <p className="truncate text-xs text-slate-500">
                                {formatDateTime(transaction.date)} | {transaction.category}
                                {transaction.counterpartyAccountNumber ? ` | ${transaction.counterpartyAccountNumber}` : ''}
                            </p>
                            {failed && transaction.failureReason && (
                                <p className="mt-0.5 truncate text-xs text-rose-600">{transaction.failureReason}</p>
                            )}
                        </div>

                        <div className="text-right">
                            <p
                                className={`text-sm font-semibold ${failed ? 'text-slate-400 line-through' : incoming ? 'text-emerald-600' : 'text-slate-900'
                                    }`}
                            >
                                {incoming ? '+' : '-'}
                                {formatMoney(transaction.amount, transaction.currency ?? 'TRY')}
                            </p>
                            {transaction.status !== 'Completed' && (
                                <div className="mt-1">
                                    <StatusBadge status={transaction.status} />
                                </div>
                            )}
                        </div>
                    </li>
                )
            })}
        </ul>
    )
}