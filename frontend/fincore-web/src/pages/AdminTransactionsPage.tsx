import { AdminTransactionsView } from '../components/AdminTransactionsView'

export function AdminTransactionsPage() {
    return (
        <AdminTransactionsView
            suspiciousOnly={false}
            title="All Transactions"
            subtitle="Every transaction across all customers, with its risk score."
        />
    )
}

export function AdminSuspiciousPage() {
    return (
        <AdminTransactionsView
            suspiciousOnly
            title="Suspicious Transactions"
            subtitle="Transactions flagged by the rule-based risk engine (score 60 or higher)."
        />
    )
}