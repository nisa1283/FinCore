import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Eye, EyeOff } from 'lucide-react'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert } from '../components/Alert'
import { Button } from '../components/Button'
import { TextField } from '../components/TextField'

export function LoginPage() {
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)

  const mutation = useMutation({
    mutationFn: () => login(email.trim(), password),
  })

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    mutation.mutate()
  }

  return (
    <div>
      <h2 className="text-2xl font-bold tracking-tight">Welcome back</h2>
      <p className="mt-1 text-sm text-slate-500">Sign in to your FinCore account.</p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-5">
        {mutation.isError && <ErrorAlert message={getErrorMessage(mutation.error)} />}

        <TextField
          id="email"
          label="Email"
          type="email"
          value={email}
          onChange={setEmail}
          placeholder="you@example.com"
          autoComplete="email"
        />

        <TextField
          id="password"
          label="Password"
          type={showPassword ? 'text' : 'password'}
          value={password}
          onChange={setPassword}
          placeholder="Your password"
          autoComplete="current-password"
          rightSlot={
            <button
              type="button"
              onClick={() => setShowPassword((value) => !value)}
              className="text-slate-400 hover:text-slate-600"
              aria-label={showPassword ? 'Hide password' : 'Show password'}
            >
              {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          }
        />

        <Button type="submit" loading={mutation.isPending} disabled={!email || !password}>
          Sign in
        </Button>
      </form>

      <p className="mt-6 text-center text-sm text-slate-500">
        Don't have an account?{' '}
        <Link to="/register" className="font-semibold text-indigo-600 hover:text-indigo-500">
          Create one
        </Link>
      </p>
    </div>
  )
}