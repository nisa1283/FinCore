import { useState } from 'react'
import { Link, NavLink, Outlet } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
    Bell,
    Flag,
    LayoutDashboard,
    ListChecks,
    LogOut,
    Menu,
    Receipt,
    Send,
    ShieldCheck,
    User,
    Users,
    Wallet,
    X,
} from 'lucide-react'
import type { ComponentType } from 'react'
import { useAuth } from '../auth/AuthContext'
import { notificationApi } from '../api/notificationApi'
import { ToastContainer } from '../notifications/ToastContainer'
import { useNotificationHub } from '../notifications/useNotificationHub'

interface NavItem {
    to: string
    label: string
    icon: ComponentType<{ className?: string }>
}

const userNav: NavItem[] = [
    { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
    { to: '/accounts', label: 'Accounts', icon: Wallet },
    { to: '/transfer', label: 'Transfer', icon: Send },
    { to: '/transactions', label: 'Transactions', icon: Receipt },
    { to: '/notifications', label: 'Notifications', icon: Bell },
    { to: '/profile', label: 'Profile', icon: User },
]

const adminNav: NavItem[] = [
    { to: '/admin', label: 'Overview', icon: ShieldCheck },
    { to: '/admin/customers', label: 'Customers', icon: Users },
    { to: '/admin/transactions', label: 'All Transactions', icon: ListChecks },
    { to: '/admin/suspicious', label: 'Suspicious', icon: Flag },
]

function NavSection({ title, items, onNavigate }: { title?: string; items: NavItem[]; onNavigate: () => void }) {
    return (
        <div>
            {title && <p className="mb-2 px-3 text-xs font-semibold uppercase tracking-wider text-slate-400">{title}</p>}
            <ul className="space-y-1">
                {items.map(({ to, label, icon: Icon }) => (
                    <li key={to}>
                        <NavLink
                            to={to}
                            end={to === '/admin'}
                            onClick={onNavigate}
                            className={({ isActive }) =>
                                `flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition ${isActive
                                    ? 'bg-indigo-50 text-indigo-700'
                                    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                                }`
                            }
                        >
                            <Icon className="h-5 w-5" />
                            {label}
                        </NavLink>
                    </li>
                ))}
            </ul>
        </div>
    )
}

export function AppLayout() {
    const { user, isAdmin, logout } = useAuth()
    const queryClient = useQueryClient()
    const { toasts, dismissToast } = useNotificationHub()
    const { data: unreadCount = 0 } = useQuery({ queryKey: ['unread-count'], queryFn: notificationApi.getUnreadCount })
    const [sidebarOpen, setSidebarOpen] = useState(false)

    const closeSidebar = () => setSidebarOpen(false)

    const handleLogout = async () => {
        await logout()
        queryClient.clear() // önceki kullanıcının verisi bellekte kalmasın
    }

    const initials = (user?.fullName ?? '')
        .split(' ')
        .filter(Boolean)
        .map((part) => part[0])
        .slice(0, 2)
        .join('')
        .toUpperCase()

    return (
        <div className="min-h-screen lg:pl-64">
            {sidebarOpen && <div className="fixed inset-0 z-30 bg-slate-900/40 lg:hidden" onClick={closeSidebar} />}

            <aside
                className={`fixed inset-y-0 left-0 z-40 flex w-64 transform flex-col border-r border-slate-200 bg-white transition-transform lg:translate-x-0 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full'
                    }`}
            >
                <div className="flex h-16 items-center justify-between border-b border-slate-100 px-5">
                    <div className="flex items-center gap-3">
                        <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-indigo-600 font-bold text-white">F</div>
                        <span className="text-lg font-semibold tracking-tight">FinCore</span>
                    </div>
                    <button onClick={closeSidebar} className="rounded-lg p-1 text-slate-500 hover:bg-slate-100 lg:hidden" aria-label="Close menu">
                        <X className="h-5 w-5" />
                    </button>
                </div>

                <nav className="flex-1 space-y-6 overflow-y-auto px-3 py-6">
                    <NavSection items={userNav} onNavigate={closeSidebar} />
                    {isAdmin && <NavSection title="Admin" items={adminNav} onNavigate={closeSidebar} />}
                </nav>
            </aside>

            <header className="sticky top-0 z-20 flex h-16 items-center justify-between border-b border-slate-200 bg-white/80 px-4 backdrop-blur sm:px-6">
                <button onClick={() => setSidebarOpen(true)} className="rounded-lg p-2 text-slate-600 hover:bg-slate-100 lg:hidden" aria-label="Open menu">
                    <Menu className="h-5 w-5" />
                </button>

                <div className="hidden lg:block" />

                <div className="flex items-center gap-3">
                    <div className="hidden text-right sm:block">
                        <p className="text-sm font-medium leading-tight">{user?.fullName}</p>
                        <p className="text-xs text-slate-500">{user?.role}</p>
                    </div>
                    <Link
                        to="/notifications"
                        className="relative rounded-lg p-2 text-slate-600 hover:bg-slate-100"
                        aria-label="Notifications"
                    >
                        <Bell className="h-5 w-5" />
                        {unreadCount > 0 && (
                            <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-rose-500 px-1 text-[10px] font-bold text-white">
                                {unreadCount > 9 ? '9+' : unreadCount}
                            </span>
                        )}
                    </Link>k
                    <div className="flex h-9 w-9 items-center justify-center rounded-full bg-indigo-100 text-sm font-semibold text-indigo-700">
                        {initials}
                    </div>
                    <button
                        onClick={handleLogout}
                        className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
                    >
                        <LogOut className="h-4 w-4" />
                        <span className="hidden sm:inline">Log out</span>
                    </button>
                </div>
            </header>

            <main className="p-4 sm:p-6 lg:p-8">
                <Outlet />
            </main>
            <ToastContainer toasts={toasts} onDismiss={dismissToast} />
        </div>
    )
}