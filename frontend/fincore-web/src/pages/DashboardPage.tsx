import { useAuth } from '../auth/AuthContext'

const placeholders = ['Total balance', 'Income', 'Expenses']

export function DashboardPage() {
  const { user } = useAuth()

  return (
    <div>
      <h1 className="text-2xl font-bold tracking-tight">Hello, {user?.fullName.split(' ')[0]} 👋</h1>
      <p className="mt-1 text-sm text-slate-500">Here is an overview of your finances.</p>

      <div className="mt-8 grid gap-4 sm:grid-cols-3">
        {placeholders.map((label) => (
          <div key={label} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-xs">
            <p className="text-sm text-slate-500">{label}</p>
            <p className="mt-2 text-2xl font-semibold text-slate-300">—</p>
          </div>
        ))}
      </div>

      <div className="mt-6 rounded-2xl border border-dashed border-slate-300 bg-white/60 p-8 text-center text-sm text-slate-500">
        Charts and recent transactions will appear here once they are connected to the API.
      </div>
    </div>
  )
}