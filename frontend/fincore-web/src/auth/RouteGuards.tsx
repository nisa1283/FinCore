import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { FullPageSpinner } from '../components/Spinner'
import { useAuth } from './AuthContext'

export function ProtectedRoute({ requireAdmin = false }: { requireAdmin?: boolean }) {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) return <FullPageSpinner />

  if (!user) {
    // Girişten sonra kullanıcıyı gitmek istediği sayfaya geri döndürmek için konumu saklıyoruz
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  if (requireAdmin && user.role !== 'Admin') {
    return <Navigate to="/dashboard" replace />
  }

  return <Outlet />
}

export function GuestRoute() {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) return <FullPageSpinner />

  if (user) {
    const from = (location.state as { from?: string } | null)?.from ?? '/dashboard'
    return <Navigate to={from} replace />
  }

  return <Outlet />
}