import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate } from 'react-router'
import { useAuthSession } from '../auth/AuthContext'
import { SessionNotice } from '../ui/SessionNotice'

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { t } = useTranslation()
  const { status } = useAuthSession()

  if (status === 'checking') {
    return <SessionNotice>{t('auth.checkingSession')}</SessionNotice>
  }

  if (status !== 'authenticated') {
    return <Navigate to="/login" replace />
  }

  return children
}
