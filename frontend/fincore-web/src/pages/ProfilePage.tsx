import { useState, type FormEvent } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Check, Circle } from 'lucide-react'
import { authApi } from '../api/authApi'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../auth/AuthContext'
import { ErrorAlert, SuccessAlert } from '../components/Alert'
import { Button } from '../components/Button'
import { TextField } from '../components/TextField'
import { Card, PageHeader } from '../components/ui'
import { passwordRules } from '../lib/passwordRules'

export function ProfilePage() {
    const { user, updateUser, logout } = useAuth()

    const [fullName, setFullName] = useState(user?.fullName ?? '')
    const [nameError, setNameError] = useState<string | null>(null)

    const [currentPassword, setCurrentPassword] = useState('')
    const [newPassword, setNewPassword] = useState('')
    const [confirmPassword, setConfirmPassword] = useState('')
    const [passwordErrors, setPasswordErrors] = useState<Record<string, string>>({})

    const profileMutation = useMutation({
        mutationFn: () => authApi.updateProfile(fullName.trim()),
        onSuccess: (profile) => updateUser(profile),
    })

    const passwordMutation = useMutation({
        mutationFn: () => authApi.changePassword(currentPassword, newPassword),
        onSuccess: () => {
            // The server revoked all sessions, so sign out locally too
            window.setTimeout(() => void logout(), 1500)
        },
    })

    const handleProfileSubmit = (event: FormEvent) => {
        event.preventDefault()

        if (!fullName.trim()) {
            setNameError('Full name is required.')
            return
        }

        setNameError(null)
        profileMutation.mutate()
    }

    const handlePasswordSubmit = (event: FormEvent) => {
        event.preventDefault()

        const found: Record<string, string> = {}

        if (!currentPassword) found.currentPassword = 'Enter your current password.'
        if (!passwordRules.every((rule) => rule.test(newPassword))) found.newPassword = 'Password does not meet the requirements.'
        else if (newPassword === currentPassword) found.newPassword = 'New password must be different from the current one.'
        if (confirmPassword !== newPassword) found.confirmPassword = 'Passwords do not match.'

        setPasswordErrors(found)
        if (Object.keys(found).length > 0) return

        passwordMutation.mutate()
    }

    return (
        <div className="mx-auto max-w-2xl">
            <PageHeader title="Profile" subtitle="Manage your personal details and security." />

            <Card>
                <h2 className="font-semibold">Personal details</h2>

                <form onSubmit={handleProfileSubmit} noValidate className="mt-5 space-y-5">
                    {profileMutation.isError && <ErrorAlert message={getErrorMessage(profileMutation.error)} />}
                    {profileMutation.isSuccess && <SuccessAlert message="Profile updated." />}

                    <TextField id="fullName" label="Full name" value={fullName} onChange={setFullName} error={nameError} />

                    <div>
                        <p className="mb-1.5 text-sm font-medium text-slate-700">Email</p>
                        <p className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-2.5 text-sm text-slate-600">{user?.email}</p>
                    </div>

                    <div>
                        <p className="mb-1.5 text-sm font-medium text-slate-700">Role</p>
                        <p className="text-sm text-slate-600">{user?.role}</p>
                    </div>

                    <div className="w-40">
                        <Button type="submit" loading={profileMutation.isPending}>
                            Save changes
                        </Button>
                    </div>
                </form>
            </Card>

            <Card className="mt-6">
                <h2 className="font-semibold">Security</h2>
                <p className="mt-1 text-sm text-slate-500">Changing your password signs you out on every device.</p>

                <form onSubmit={handlePasswordSubmit} noValidate className="mt-5 space-y-5">
                    {passwordMutation.isError && <ErrorAlert message={getErrorMessage(passwordMutation.error)} />}
                    {passwordMutation.isSuccess && <SuccessAlert message="Password changed. Signing you out..." />}

                    <TextField
                        id="currentPassword"
                        label="Current password"
                        type="password"
                        value={currentPassword}
                        onChange={setCurrentPassword}
                        autoComplete="current-password"
                        error={passwordErrors.currentPassword}
                    />

                    <div>
                        <TextField
                            id="newPassword"
                            label="New password"
                            type="password"
                            value={newPassword}
                            onChange={setNewPassword}
                            autoComplete="new-password"
                            error={passwordErrors.newPassword}
                        />

                        <ul className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1.5">
                            {passwordRules.map((rule) => {
                                const passed = rule.test(newPassword)
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
                        label="Confirm new password"
                        type="password"
                        value={confirmPassword}
                        onChange={setConfirmPassword}
                        autoComplete="new-password"
                        error={passwordErrors.confirmPassword}
                    />

                    <div className="w-48">
                        <Button type="submit" loading={passwordMutation.isPending} disabled={passwordMutation.isSuccess}>
                            Change password
                        </Button>
                    </div>
                </form>
            </Card>
        </div>
    )
}