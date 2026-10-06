import { useEffect, type ReactNode } from 'react'
import { ChevronLeft, ChevronRight, X } from 'lucide-react'
import { getErrorMessage } from '../api/errors'

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
    return <div className={`rounded-2xl border border-slate-200 bg-white p-5 shadow-xs ${className}`}>{children}</div>
}

export function PageHeader({ title, subtitle, action }: { title: string; subtitle?: string; action?: ReactNode }) {
    return (
        <div className="mb-6 flex flex-wrap items-start justify-between gap-3">
            <div>
                <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
                {subtitle && <p className="mt-1 text-sm text-slate-500">{subtitle}</p>}
            </div>
            {action}
        </div>
    )
}

const badgeStyles: Record<string, string> = {
    Completed: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20',
    Active: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20',
    Pending: 'bg-amber-50 text-amber-700 ring-amber-600/20',
    Failed: 'bg-rose-50 text-rose-700 ring-rose-600/20',
    Frozen: 'bg-sky-50 text-sky-700 ring-sky-600/20',
    Inactive: 'bg-rose-50 text-rose-700 ring-rose-600/20',
    Locked: 'bg-amber-50 text-amber-700 ring-amber-600/20',
}

export function StatusBadge({ status }: { status: string }) {
    return (
        <span
            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${badgeStyles[status] ?? 'bg-slate-50 text-slate-600 ring-slate-500/20'
                }`}
        >
            {status}
        </span>
    )
}

export function EmptyState({ title, message, action }: { title: string; message?: string; action?: ReactNode }) {
    return (
        <div className="flex flex-col items-center px-6 py-12 text-center">
            <p className="font-medium">{title}</p>
            {message && <p className="mt-1 max-w-sm text-sm text-slate-500">{message}</p>}
            {action && <div className="mt-4">{action}</div>}
        </div>
    )
}

export function LoadingBlock({ className = 'h-24' }: { className?: string }) {
    return <div className={`animate-pulse rounded-2xl bg-slate-200/70 ${className}`} />
}

export function QueryError({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
    return (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-5 text-sm text-rose-700">
            <p>{getErrorMessage(error)}</p>
            {onRetry && (
                <button onClick={onRetry} className="mt-2 font-semibold underline">
                    Try again
                </button>
            )}
        </div>
    )
}

export function Pagination({
    page,
    totalPages,
    onChange,
}: {
    page: number
    totalPages: number
    onChange: (page: number) => void
}) {
    if (totalPages <= 1) return null

    return (
        <div className="mt-4 flex items-center justify-between border-t border-slate-100 pt-4">
            <p className="text-sm text-slate-500">
                Page {page} of {totalPages}
            </p>
            <div className="flex gap-2">
                <button
                    onClick={() => onChange(page - 1)}
                    disabled={page <= 1}
                    className="flex items-center gap-1 rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-medium hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
                >
                    <ChevronLeft className="h-4 w-4" /> Prev
                </button>
                <button
                    onClick={() => onChange(page + 1)}
                    disabled={page >= totalPages}
                    className="flex items-center gap-1 rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-medium hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
                >
                    Next <ChevronRight className="h-4 w-4" />
                </button>
            </div>
        </div>
    )
}

export function Modal({
    open,
    onClose,
    title,
    children,
}: {
    open: boolean
    onClose: () => void
    title: string
    children: ReactNode
}) {
    useEffect(() => {
        if (!open) return

        const onKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape') onClose()
        }

        window.addEventListener('keydown', onKeyDown)
        return () => window.removeEventListener('keydown', onKeyDown)
    }, [open, onClose])

    if (!open) return null

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="absolute inset-0 bg-slate-900/50" onClick={onClose} />
            <div className="relative w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
                <div className="mb-5 flex items-center justify-between">
                    <h2 className="text-lg font-semibold">{title}</h2>
                    <button onClick={onClose} className="rounded-lg p-1 text-slate-400 hover:bg-slate-100" aria-label="Close">
                        <X className="h-5 w-5" />
                    </button>
                </div>
                {children}
            </div>
        </div>
    )
}

interface SelectFieldProps {
    id: string
    label: string
    value: string
    onChange: (value: string) => void
    options: { value: string; label: string }[]
    error?: string | null
}

export function SelectField({ id, label, value, onChange, options, error }: SelectFieldProps) {
    return (
        <div>
            <label htmlFor={id} className="mb-1.5 block text-sm font-medium text-slate-700">
                {label}
            </label>
            <select
                id={id}
                value={value}
                onChange={(event) => onChange(event.target.value)}
                className={`block w-full rounded-xl border bg-white px-4 py-2.5 text-sm text-slate-900 shadow-xs focus:outline-none focus:ring-2 ${error ? 'border-rose-400 focus:ring-rose-200' : 'border-slate-300 focus:border-indigo-500 focus:ring-indigo-200'
                    }`}
            >
                {options.map((option) => (
                    <option key={option.value} value={option.value}>
                        {option.label}
                    </option>
                ))}
            </select>
            {error && <p className="mt-1.5 text-xs text-rose-600">{error}</p>}
        </div>
    )
}