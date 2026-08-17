import { Navigate } from 'react-router'
import type { ReactNode } from 'react'
import { useAuthSession } from '../auth/AuthContext'

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { status } = useAuthSession()

  if (status === 'checking') {
    return <p>Checking session…</p>
  }

  if (status !== 'authenticated') {
    return <Navigate to="/login" replace />
  }

  return children
}
