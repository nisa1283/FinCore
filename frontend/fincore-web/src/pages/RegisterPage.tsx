import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { Check, Circle } from 'lucide-react'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert } from '../components/Alert'
import { Button } from '../components/Button'
import { TextField } from '../components/TextField'

const passwordRules = [
  { label: 'At least 8 characters', test: (value: string) => value.length >= 8 },
  { label: 'One uppercase letter', test: (value: string) => /[A-Z]/.test(value) },
  { label: 'One lowercase letter', test: (value: string) => /[a-z]/.test(value) },
  { label: 'One digit', test: (value: string) => /[0-9]/.test(value) },
]

export function RegisterPage() {
  const { register } = useAuth()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})

  const mutation = useMutation({
    mutationFn: () => register(fullName.trim(), email.trim(), password),
  })

  const validate = () => {
    const result: Record<string, string> = {}

    if (!fullName.trim()) result.fullName = 'Full name is required.'
    if (!/^\S+@\S+\.\S+$/.test(email.trim())) result.email = 'Enter a valid email address.'
    if (!passwordRules.every((rule) => rule.test(password))) result.password = 'Password does not meet the requirements.'
    if (confirmPassword !== password) result.confirmPassword = 'Passwords do not match.'

    return result
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()

    const validationErrors = validate()
    setErrors(validationErrors)

    if (Object.keys(validationErrors).length > 0) return

    mutation.mutate()
  }

  return (
    <div>
      <h2 className="text-2xl font-bold tracking-tight">Create your account</h2>
      <p className="mt-1 text-sm text-slate-500">It only takes a minute.</p>

      <form onSubmit={handleSubmit} noValidate className="mt-8 space-y-5">
        {mutation.isError && <ErrorAlert message={getErrorMessage(mutation.error)} />}

        <TextField
          id="fullName"
          label="Full name"
          value={fullName}
          onChange={setFullName}
          placeholder="Jane Doe"
          autoComplete="name"
          error={errors.fullName}
        />

        <TextField
          id="email"
          label="Email"
          type="email"
          value={email}
          onChange={setEmail}
          placeholder="you@example.com"
          autoComplete="email"
          error={errors.email}
        />

        <div>
          <TextField
            id="password"
            label="Password"
            type="password"
            value={password}
            onChange={setPassword}
            placeholder="Create a password"
            autoComplete="new-password"
            error={errors.password}
          />

          <ul className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1.5">
            {passwordRules.map((rule) => {
              const passed = rule.test(password)
              return (
                <li key={rule.label} className={`flex items-center gap-1.5 text-xs ${passed ? 'text-emerald-600' : 'text-slate-400'}`}>
                  {passed ? <Check className="h-3.5 w-3.5" /> : <Circle className="h-3 w-3" />}
                  {rule.label}
                </li>
              )
            })}
          </ul>
        </div>

        <TextField
          id="confirmPassword"
          label="Confirm password"
          type="password"
          value={confirmPassword}
          onChange={setConfirmPassword}
          placeholder="Repeat your password"
          autoComplete="new-password"
          error={errors.confirmPassword}
        />

        <Button type="submit" loading={mutation.isPending}>
          Create account
        </Button>
      </form>

      <p className="mt-6 text-center text-sm text-slate-500">
        Already have an account?{' '}
        <Link to="/login" className="font-semibold text-indigo-600 hover:text-indigo-500">
          Sign in
        </Link>
      </p>
    </div>
  )
}