import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { AuthContext, type AuthContextValue, type AuthStatus, type PreferenceError } from './AuthContext'
import { fetchCsrfToken, fetchSession, login as loginRequest, logout as logoutRequest, selectProperty as selectPropertyRequest } from './sessionApi'
import {
  applyLanguage,
  currentLanguage,
  persistAuthenticatedLanguage,
  reconcileAuthenticatedLanguage,
} from '../i18n/preference'
import type { AppLanguage } from '../i18n/languages'
import type { CurrentUser } from '../shared/types'

export function AuthSessionProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('checking')
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [preferenceError, setPreferenceError] = useState<PreferenceError>(null)

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
          const reconciled = await reconcileAuthenticatedLanguage(session.user.preferredLanguage, {
            consumeExplicit: false,
          })
          if (cancelled) {
            return
          }

          setUser(reconciled.user ?? session.user)
          setPreferenceError(reconciled.saveFailed ? 'unsaved' : null)
          setStatus('authenticated')
        } else {
          setUser(null)
          setPreferenceError(null)
          setStatus('anonymous')
        }
      } catch {
        if (!cancelled) {
          setUser(null)
          setPreferenceError(null)
          setStatus('anonymous')
        }
      }
    }

    void loadSession()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    function onFocus() {
      if (status !== 'authenticated') {
        return
      }

      void fetchSession()
        .then((session) => {
          if (session.authenticated && session.user) {
            setUser(session.user)
          }
        })
        .catch(() => undefined)
    }

    window.addEventListener('focus', onFocus)
    return () => window.removeEventListener('focus', onFocus)
  }, [status])

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      preferenceError,
      signIn: async (email, password) => {
        const signedIn = await loginRequest(email, password)
        await fetchCsrfToken()
        const reconciled = await reconcileAuthenticatedLanguage(signedIn.preferredLanguage, {
          consumeExplicit: true,
        })
        setUser(reconciled.user ?? signedIn)
        setPreferenceError(reconciled.saveFailed ? 'unsaved' : null)
        setStatus('authenticated')
      },
      signOut: async () => {
        await logoutRequest()
        await fetchCsrfToken()
        setUser(null)
        setPreferenceError(null)
        setStatus('anonymous')
      },
      updatePreferredLanguage: async (language: AppLanguage) => {
        const previous = currentLanguage()
        try {
          const updated = await persistAuthenticatedLanguage(language)
          setUser(updated)
          setPreferenceError(null)
        } catch {
          await applyLanguage(previous)
          setPreferenceError('reverted')
        }
      },
      selectProperty: async (propertyId: string) => {
        const updated = await selectPropertyRequest(propertyId)
        setUser(updated)
      },
    }),
    [status, user, preferenceError],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
