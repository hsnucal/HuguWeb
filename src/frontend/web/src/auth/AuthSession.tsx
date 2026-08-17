import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { AuthContext, type AuthContextValue, type AuthStatus } from './AuthContext'
import { fetchCsrfToken, fetchSession, login as loginRequest, logout as logoutRequest } from './sessionApi'
import type { CurrentUser } from '../shared/types'

export function AuthSessionProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('checking')
  const [user, setUser] = useState<CurrentUser | null>(null)

  useEffect(() => {
    let cancelled = false

    async function loadSession() {
      try {
        await fetchCsrfToken()
        const session = await fetchSession()
        if (cancelled) {
          return
        }

        if (session.authenticated && session.user) {
          setUser(session.user)
          setStatus('authenticated')
        } else {
          setUser(null)
          setStatus('anonymous')
        }
      } catch {
        if (!cancelled) {
          setUser(null)
          setStatus('anonymous')
        }
      }
    }

    void loadSession()
    return () => {
      cancelled = true
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      signIn: async (email, password) => {
        const signedIn = await loginRequest(email, password)
        await fetchCsrfToken()
        setUser(signedIn)
        setStatus('authenticated')
      },
      signOut: async () => {
        await logoutRequest()
        await fetchCsrfToken()
        setUser(null)
        setStatus('anonymous')
      },
    }),
    [status, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
