import { Navigate, Route, Routes } from 'react-router-dom'
import { GuestRoute, ProtectedRoute } from './auth/RouteGuards'
import { AppLayout } from './layouts/AppLayout'
import { AuthLayout } from './layouts/AuthLayout'
import { ComingSoonPage } from './pages/ComingSoonPage'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RegisterPage } from './pages/RegisterPage'

export default function App() {
  return (
    <Routes>
      {/* Giriş yapmamış kullanıcılar */}
      <Route element={<GuestRoute />}>
        <Route element={<AuthLayout />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
        </Route>
      </Route>

      {/* Giriş yapmış kullanıcılar */}
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/accounts" element={<ComingSoonPage title="Accounts" />} />
          <Route path="/transfer" element={<ComingSoonPage title="Transfer" />} />
          <Route path="/transactions" element={<ComingSoonPage title="Transactions" />} />
          <Route path="/notifications" element={<ComingSoonPage title="Notifications" />} />
          <Route path="/profile" element={<ComingSoonPage title="Profile" />} />

          {/* Sadece Admin */}
          <Route element={<ProtectedRoute requireAdmin />}>
            <Route path="/admin" element={<ComingSoonPage title="Admin Overview" />} />
            <Route path="/admin/customers" element={<ComingSoonPage title="Customers" />} />
            <Route path="/admin/transactions" element={<ComingSoonPage title="All Transactions" />} />
            <Route path="/admin/suspicious" element={<ComingSoonPage title="Suspicious Transactions" />} />
          </Route>
        </Route>
      </Route>

      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}