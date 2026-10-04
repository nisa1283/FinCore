import type { ReactNode } from 'react'
import { Outlet } from 'react-router-dom'
import { ShieldCheck, TrendingUp, Zap } from 'lucide-react'

function Feature({ icon, text }: { icon: ReactNode; text: string }) {
  return (
    <li className="flex items-center gap-3">
      <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-white/15">{icon}</span>
      <span className="text-indigo-50">{text}</span>
    </li>
  )
}

function Logo({ dark = false }: { dark?: boolean }) {
  return (
    <div className="flex items-center gap-3">
      <div
        className={`flex h-10 w-10 items-center justify-center rounded-xl text-lg font-bold ${
          dark ? 'bg-indigo-600 text-white' : 'bg-white/15 text-white'
        }`}
      >
        F
      </div>
      <span className={`text-xl font-semibold tracking-tight ${dark ? 'text-slate-900' : 'text-white'}`}>FinCore</span>
    </div>
  )
}

export function AuthLayout() {
  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      <div className="relative hidden overflow-hidden bg-linear-to-br from-indigo-700 via-indigo-600 to-violet-700 p-12 text-white lg:flex lg:flex-col lg:justify-between">
        <div className="absolute -right-24 -top-24 h-72 w-72 rounded-full bg-white/10" />
        <div className="absolute -bottom-32 -left-20 h-80 w-80 rounded-full bg-white/10" />

        <div className="relative">
          <Logo />
        </div>

        <div className="relative">
          <h1 className="text-4xl font-bold leading-tight">
            Modern banking,
            <br />
            built for clarity.
          </h1>
          <p className="mt-4 max-w-md text-indigo-100">
            Manage your accounts, send money and keep an eye on your spending in one secure place.
          </p>

          <ul className="mt-10 space-y-4 text-sm">
            <Feature icon={<ShieldCheck className="h-5 w-5" />} text="Secure sign-in with JWT and refresh tokens" />
            <Feature icon={<Zap className="h-5 w-5" />} text="Real-time transfer notifications" />
            <Feature icon={<TrendingUp className="h-5 w-5" />} text="Clear income and spending insights" />
          </ul>
        </div>

        <p className="relative text-xs text-indigo-200">Demo project. No real money is involved.</p>
      </div>

      <div className="flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-md">
          <div className="mb-8 lg:hidden">
            <Logo dark />
          </div>
          <Outlet />
        </div>
      </div>
    </div>
  )
}