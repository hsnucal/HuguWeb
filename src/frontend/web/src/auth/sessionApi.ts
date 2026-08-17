import { apiRequest, setCsrfToken } from '../shared/apiClient'
import type { CsrfResponse, CurrentUser, SessionResponse } from '../shared/types'

export async function fetchCsrfToken(): Promise<void> {
  const response = await apiRequest<CsrfResponse>('/api/auth/csrf')
  setCsrfToken(response.token)
}

export async function fetchSession(): Promise<SessionResponse> {
  return apiRequest<SessionResponse>('/api/auth/session')
}

export async function login(email: string, password: string): Promise<CurrentUser> {
  return apiRequest<CurrentUser>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })
}

export async function logout(): Promise<void> {
  await apiRequest<void>('/api/auth/logout', { method: 'POST' })
  setCsrfToken(null)
}

export async function updatePreferredLanguage(language: string): Promise<CurrentUser> {
  return apiRequest<CurrentUser>('/api/auth/preferences/language', {
    method: 'PATCH',
    body: JSON.stringify({ language }),
  })
}
