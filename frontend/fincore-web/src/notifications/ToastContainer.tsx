import { Bell, X } from 'lucide-react'
import type { Toast } from './useNotificationHub'

export function ToastContainer({ toasts, onDismiss }: { toasts: Toast[]; onDismiss: (id: string) => void }) {
    return (
        <div className="pointer-events-none fixed right-4 top-4 z-50 flex w-80 max-w-[calc(100vw-2rem)] flex-col gap-3">
            {toasts.map((toast) => (
                <div
                    key={toast.id}
                    className="pointer-events-auto flex gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-lg"
                >
                    <div className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-indigo-50 text-indigo-600">
                        <Bell className="h-4 w-4" />
                    </div>
                    <div className="min-w-0 flex-1">
                        <p className="text-sm font-semibold">{toast.title}</p>
                        <p className="mt-0.5 text-sm text-slate-500">{toast.message}</p>
                    </div>
                    <button
                        onClick={() => onDismiss(toast.id)}
                        className="self-start text-slate-400 hover:text-slate-600"
                        aria-label="Dismiss"
                    >
                        <X className="h-4 w-4" />
                    </button>
                </div>
            ))}
        </div>
    )
}