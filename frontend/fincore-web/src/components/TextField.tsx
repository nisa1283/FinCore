import type { ReactNode } from 'react'

interface TextFieldProps {
  id: string
  label: string
  value: string
  onChange: (value: string) => void
  type?: string
  placeholder?: string
  autoComplete?: string
  error?: string | null
  rightSlot?: ReactNode
}

export function TextField({
  id,
  label,
  value,
  onChange,
  type = 'text',
  placeholder,
  autoComplete,
  error,
  rightSlot,
}: TextFieldProps) {
  return (
    <div>
      <label htmlFor={id} className="mb-1.5 block text-sm font-medium text-slate-700">
        {label}
      </label>

      <div className="relative">
        <input
          id={id}
          type={type}
          value={value}
          placeholder={placeholder}
          autoComplete={autoComplete}
          aria-invalid={!!error}
          onChange={(event) => onChange(event.target.value)}
          className={`block w-full rounded-xl border bg-white px-4 py-2.5 text-sm text-slate-900 shadow-xs placeholder:text-slate-400 focus:outline-none focus:ring-2 ${
            rightSlot ? 'pr-11' : ''
          } ${
            error
              ? 'border-rose-400 focus:ring-rose-200'
              : 'border-slate-300 focus:border-indigo-500 focus:ring-indigo-200'
          }`}
        />

        {rightSlot && <div className="absolute inset-y-0 right-0 flex items-center pr-3">{rightSlot}</div>}
      </div>

      {error && <p className="mt-1.5 text-xs text-rose-600">{error}</p>}
    </div>
  )
}