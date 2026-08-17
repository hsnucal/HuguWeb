import { createContext, useContext } from 'react'
import type { AppLanguage } from '../i18n/languages'
import type { CurrentUser } from '../shared/types'

export type AuthStatus = 'checking' | 'anonymous' | 'authenticated'

export type PreferenceError = 'reverted' | 'unsaved' | null

export type AuthContextValue = {
  status: AuthStatus
  user: CurrentUser | null
  preferenceError: PreferenceError
  signIn: (email: string, password: string) => Promise<void>
  signOut: () => Promise<void>
  updatePreferredLanguage: (language: AppLanguage) => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuthSession() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuthSession must be used within AuthSessionProvider')
  }

  return context
}
