import type { ReactNode } from 'react'
import { Navigate } from 'react-router'
import { useAuthSession } from '../auth/AuthContext'
import { SessionNotice } from '../ui/SessionNotice'

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { status } = useAuthSession()

  if (status === 'checking') {
    return <SessionNotice>Checking session…</SessionNotice>
  }

  if (status !== 'authenticated') {
    return <Navigate to="/login" replace />
  }

  return children
}
