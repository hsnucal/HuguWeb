import { Navigate, Route, Routes } from 'react-router'
import { AppShell } from './AppShell'
import { ProtectedRoute } from './ProtectedRoute'
import { LoginPage } from '../auth/LoginPage'

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/app"
        element={
          <ProtectedRoute>
            <AppShell />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/app" replace />} />
    </Routes>
  )
}
