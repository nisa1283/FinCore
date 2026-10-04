import { Hammer } from 'lucide-react'

export function ComingSoonPage({ title }: { title: string }) {
  return (
    <div>
      <h1 className="text-2xl font-bold tracking-tight">{title}</h1>

      <div className="mt-8 flex flex-col items-center rounded-2xl border border-dashed border-slate-300 bg-white/60 px-6 py-16 text-center">
        <div className="flex h-12 w-12 items-center justify-center rounded-full bg-indigo-50 text-indigo-600">
          <Hammer className="h-6 w-6" />
        </div>
        <p className="mt-4 font-medium">This page is under construction</p>
        <p className="mt-1 text-sm text-slate-500">It will be connected to the API soon.</p>
      </div>
    </div>
  )
}