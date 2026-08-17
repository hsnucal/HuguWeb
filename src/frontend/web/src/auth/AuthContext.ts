import { createContext, useContext } from 'react'
import type { CurrentUser } from '../shared/types'

export type AuthStatus = 'checking' | 'anonymous' | 'authenticated'

export type AuthContextValue = {
  status: AuthStatus
  user: CurrentUser | null
  signIn: (email: string, password: string) => Promise<void>
  signOut: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuthSession() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuthSession must be used within AuthSessionProvider')
  }

  return context
}
