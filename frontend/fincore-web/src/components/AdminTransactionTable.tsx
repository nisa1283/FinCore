import type { AdminTransactionItem } from '../api/types'
import { formatDateTime, formatMoney } from '../lib/format'
import { StatusBadge } from './ui'

function RiskBadge({ score, reasons }: { score: number; reasons: string | null }) {
    const tone =
        score >= 60
            ? 'bg-rose-50 text-rose-700 ring-rose-600/20'
            : score >= 30
                ? 'bg-amber-50 text-amber-700 ring-amber-600/20'
                : 'bg-slate-50 text-slate-600 ring-slate-500/20'

    return (
        <div>
            <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold ring-1 ring-inset ${tone}`}>
                {score}
            </span>
            {reasons && <p className="mt-1 text-xs text-slate-500">{reasons.split(',').join(', ')}</p>}
        </div>
    )
}

export function AdminTransactionTable({ items }: { items: AdminTransactionItem[] }) {
    return (
        <div className="overflow-x-auto">
            <table className="w-full min-w-[880px] text-left text-sm">
                <thead>
                    <tr className="border-b border-slate-100 text-xs uppercase tracking-wider text-slate-400">
                        <th className="py-3 pr-4 font-medium">Date</th>
                        <th className="py-3 pr-4 font-medium">From / To</th>
                        <th className="py-3 pr-4 font-medium">Amount</th>
                        <th className="py-3 pr-4 font-medium">Category</th>
                        <th className="py-3 pr-4 font-medium">Status</th>
                        <th className="py-3 font-medium">Risk</th>
                    </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                    {items.map((item) => (
                        <tr key={item.id} className={item.isSuspicious ? 'bg-rose-50/40' : ''}>
                            <td className="whitespace-nowrap py-3 pr-4 text-slate-600">{formatDateTime(item.date)}</td>

                            <td className="py-3 pr-4">
                                <p className="font-mono text-xs" title={`Sender user: ${item.senderUserId}`}>
                                    {item.sourceAccountNumber ?? 'n/a'}
                                </p>
                                <p
                                    className="font-mono text-xs text-slate-500"
                                    title={item.receiverUserId ? `Receiver user: ${item.receiverUserId}` : ''}
                                >
                                    to {item.targetAccountNumber}
                                </p>
                            </td>

                            <td className="whitespace-nowrap py-3 pr-4 font-semibold">
                                {formatMoney(item.amount, item.currency ?? 'TRY')}
                            </td>

                            <td className="py-3 pr-4 text-slate-600">{item.category}</td>

                            <td className="py-3 pr-4">
                                <StatusBadge status={item.status} />
                                {item.failureReason && <p className="mt-1 text-xs text-rose-600">{item.failureReason}</p>}
                            </td>

                            <td className="py-3">
                                <RiskBadge score={item.riskScore} reasons={item.riskReasons} />
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    )
}